# Project memory

`project_memory.json` records this solution's architectural decisions. A Claude Code
`PreToolUse` hook (`mneme-hook`) checks every Edit/Write/MultiEdit against it and blocks
edits that violate a decision.

## How matching actually works

Two different tokenizers run, and the difference is the whole game.

**Retrieval** — which decisions get checked. The hook queries `edit to <absolute file path>`.
That string is lowercased, split on non-alphanumerics, and tokens shorter than 4 characters
are dropped. Tokens are matched against each decision's `scope` (weight 2.0), `constraints`
and `anti_patterns` (1.5), `decision` (1.0) and `rationale` (0.5). The top 3 non-zero
scoring decisions are enforced.

So `src\BackupHelper.Core\Sinks\Pruner.cs` yields
`{edit, programming, personal, backuphelper, core, sinks, pruner}`.
That is why `scope: ["core"]` reaches everything under `BackupHelper.Core`.

**Enforcement** — what counts as a violation. Every `anti_patterns` entry is split into
words (min 3 chars, minus a small stopword list) and each word is matched **whole-word,
case-insensitive, against the entire post-edit file content** — not the diff. A `constraints`
entry only enforces if it starts with `no ` (and it produces WARN, which *also blocks* in
strict mode).

## Rules for adding a decision

1. **Anti-patterns must be single distinctive tokens.** `"serilog"`, `"dbcontext"`.
   Never a sentence: `"do not add an external cache service"` makes `service` a trigger,
   and `service` appears in half the codebase.

2. **Verify the token is absent before adding it.** Grep first. If it already appears
   somewhere legitimate, that file becomes permanently uneditable while the decision is
   in the top 3 for it.

3. **Never write these words in `decision`, `rationale`, `scope`, `constraints` or
   `anti_patterns`:**

   `backuphelper`, `programming`, `personal`, `edit` — they appear in *every* query, so
   the decision would score non-zero on every file and be enforced globally.

   `mneme`, `project`, `memory`, `json` — they appear in this file's own path. A decision
   matching them gets enforced against `project_memory.json` itself, which contains every
   anti-pattern token by construction, so the file can no longer be edited. (Plural forms
   like `projects` are distinct tokens and are safe.)

4. **A decision with empty `anti_patterns` and no `no ...` constraint never blocks.** Use
   that for guidance you want surfaced by `/mneme-context` but not enforced — `bh_005` and
   `bh_006` are deliberately advisory.

5. **`items` entries can enforce too** — read the next section before adding one.

## What `items` entries actually do

`MemoryStore.load()` converts two of the six item types into `Decision` objects at load
time (`memory_store.py:100`). The other four are documentation only and can never block.

| `type` | Becomes a Decision? | What can fire |
|---|---|---|
| `rule` | yes | `content`, and **only if it starts with `no `** → WARN |
| `anti_pattern` | yes | **`title`** — every word becomes a FAIL trigger; `content` also, under the same `no ` rule |
| `fact` | no | nothing |
| `preference` | no | nothing |
| `architecture_decision` | no | nothing |
| `example` | no | nothing |

The exact conversion:

```python
if item.type == "rule":
    Decision(decision=item.title, scope=["general"], constraints=[item.content])

elif item.type == "anti_pattern":
    Decision(decision=f"Avoid: {item.title}", scope=["general"],
             constraints=[item.content], anti_patterns=[item.title])
```

Three consequences:

- **A `rule` item is inert unless its `content` starts with `no `.** `rule-001` here begins
  "The generated zip is a staging file...", so it is retrieved but can never fire. Rewording
  it to open with "No staging archives..." would arm every remaining word in that sentence
  as a WARN trigger — `staging`, `archives`, and the rest.

- **Avoid `type: anti_pattern` altogether.** The *title* is copied into `anti_patterns`,
  where every word of 3+ characters becomes a FAIL trigger. A title like "Using Redis for
  the session cache" arms `using`, `redis`, `session` and `cache` across the whole codebase.
  Put real bans in `decisions`, where you choose the tokens deliberately.

- **Migrated items get `scope: ["general"]`** — a token that appears in no path in this
  repo, so they have no scope-based targeting whatsoever. They retrieve only through their
  title (weight 1.0) and content (weight 1.5), which is why `rule-001` currently surfaces
  on 25 files it has nothing to do with.

## Before you commit a change to this file

Re-run the sweep — it checks every source file against the current memory and reports
anything that would be blocked. The codebase complies today, so any non-PASS is a false
positive, and the script exits non-zero so it can gate a commit:

```powershell
& "$env:LOCALAPPDATA\Programs\Python\Python313\python.exe" tools\validate_mneme.py
```

Use that interpreter explicitly: `python` on PATH resolves to msys2's build, which does
not have `mneme` installed. Python313 has it as an editable install pointing at
`E:\Programming\others\mneme`, so pulling that repo updates the checker too.

`docs/adr` is excluded from the sweep. An ADR spells out the tokens its own Constraints
section bans, so the decision covering that topic always matches the document motivating
it — those hits are false positives by construction, and leaving them in would mask a
real one. The run prints how many files it skipped so the exclusion stays visible.

The consequence to remember: **the hook still checks ADR files.** Editing anything under
`docs/adr` that names a banned token will be blocked in strict mode even though the sweep
is green. Switch `MNEME_HOOK_MODE` to `warn` for those edits, or edit them outside Claude
Code.

## Modes

`.claude/settings.json` sets `MNEME_HOOK_MODE`. `strict` blocks (current setting); `warn`
reports without blocking. Changing it requires restarting Claude Code.

The hook **fails open** — if `mneme-hook` is missing from PATH, the file can't be read, or
the check times out, the edit is allowed. "Nothing was blocked" is therefore not proof that
enforcement is working.

## The hook finds this file from the session working directory

`mneme-hook` walks up from Claude Code's **cwd**, not from the edited file's path. Launch
Claude Code from `E:\Programming\personal\BackupHelper`. Started from a parent directory,
it finds no memory and silently allows everything. `MNEME_MEMORY` can pin the path
explicitly if you need to work from elsewhere.
