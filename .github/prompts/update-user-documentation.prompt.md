---
description: "Create or update user-facing BackupHelper documentation"
agent: Ask
---
Use the User-Facing Documentation Specialist agent.

Task:
Create or update user-facing documentation for BackupHelper based on the current implementation.

Inputs (fill these before running):
- Doc objective: <README update | setup guide | usage guide | backup plan reference | troubleshooting | release notes>
- Target audience: <new user | operator | contributor>
- Behavior scope: <what changed or what to document>
- Source of truth files: <paths in src/, tests/, and docs>
- Target doc files: <paths>
- Must-keep wording/sections: <optional>
- Constraints: <length, tone, examples required>

Requirements:
1. Verify claims against code/tests before writing them.
2. Keep docs task-oriented and easy to scan.
3. Include practical command and JSON examples where relevant.
4. Do not include secrets or sensitive values.
5. Clearly label assumptions if behavior is unclear.
6. Keep terminology consistent with BackupPlan/source/sink implementations.
7. Keep edits minimal and aligned with existing document style unless rewrite is requested.

Process:
1. Provide a short documentation plan.
2. Implement the doc edits.
3. Validate command/snippet accuracy where feasible.
4. Summarize updates and remaining documentation gaps.

Output format:
- Plan
- Changes made
- Validation results
- Follow-up recommendations