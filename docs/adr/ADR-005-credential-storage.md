---
id: ADR-005
title: Secrets live only in KeePass-backed credential profiles
status: accepted
priority: foundational
date: 2026-08-07
scope: credentials.secrets.profiles
supersedes: []
---

## Context

Running a backup requires SMB share passwords and Azure account credentials.
Those values have to be available unattended, which rules out prompting, and
they have to survive being read by whoever can read the plan file, which is the
crux.

Plan files are the artifact most likely to be copied between machines, pasted
into a chat, or committed to a repository. They describe *what* to back up and
*where* to send it, so they get shared for entirely legitimate reasons.

The quality attribute is **confidentiality**, and the threat is mundane: not an
attacker on the wire, but a credential that leaks because it was sitting in a
file nobody thought of as sensitive.

## Decision

Secret values live only in KeePass-backed credential profiles under
LOCALAPPDATA, protected by a master password. Plan files and backup profiles
carry references — an SMB server and share name, or an Azure account name —
which resolve against the active credential profile at runtime.

## Consequences

An unattended run needs the credential profile unlocked, which constrains how
the tool can be scheduled. This is a real limitation and is accepted.

A plan file alone is insufficient to run a backup. Moving a plan to a new
machine requires provisioning credentials there too.
