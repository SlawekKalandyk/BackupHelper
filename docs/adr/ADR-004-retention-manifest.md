---
id: ADR-004
title: Retention state lives in a manifest file beside the archives, not in a datastore
status: accepted
priority: normal
date: 2026-08-07
scope: retention.manifest.pruning.sinks
supersedes: []
---

## Context

Each destination needs to know which archives exist and in what order, so that
pruning can keep the most recent N. That is durable state, and durable state
invites a datastore.

Two quality attributes argue against one:

- **Deployability.** The utility ships as a self-contained executable that an
  operator drops onto a workstation or small server. Any component needing
  separate installation, a service account, or a migration step at upgrade
  makes the nightly install story materially worse.
- **Recoverability.** Retention state that lives apart from the archives can
  disagree with them. A manifest written beside the data travels with the
  data, survives a destination being restored from elsewhere, and can be
  reconstructed by listing the container.

## Decision

Each destination carries a manifest file alongside its archives, recording the
archives it holds. Pruning reads and rewrites that manifest. There is no
database and no object-relational mapper anywhere in the codebase.

## Consequences

Concurrent writers to one destination can race on the manifest. Accepted: runs
are scheduled and single-writer by design.

Retention queries across destinations require reading every manifest; there is
no index. With the expected archive counts this is not a concern.

## Constraints

- FORBID_DEPENDENCY: entityframeworkcore
- FORBID_DEPENDENCY: dbcontext
- FORBID_DEPENDENCY: dapper
- FORBID_DEPENDENCY: sqlite
