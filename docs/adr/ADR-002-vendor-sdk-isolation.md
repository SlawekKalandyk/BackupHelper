---
id: ADR-002
title: Vendor storage SDK types stay behind the connector assemblies
status: accepted
priority: foundational
date: 2026-08-07
scope: core.sinks.sources.abstractions.transport
supersedes: []
---

## Context

The utility moves bytes over three transports: the local filesystem, SMB, and
Azure Blob Storage. Two of those are third-party SDKs with independent release
cadences and breaking-change histories.

The quality attribute at stake is **modifiability**. An SMB library upgrade or
a move to a different blob client should be a change with a known blast radius.
If SDK types leak into orchestration, retention, or the archive builder, the
blast radius becomes "wherever bytes are touched", which is most of the
codebase.

A secondary driver is **testability**: layers that depend only on the shared
interfaces can be exercised with hand-written doubles, without a live share or
a storage account.

## Decision

Vendor SDK types are confined to the connector assemblies. Every other layer
reaches storage through the shared interfaces defined in the abstractions
assembly.

## Consequences

Each transport needs a wrapper type that adds no behaviour beyond adapting the
vendor surface, which is boilerplate. Accepted as the price of a bounded
upgrade path.

Features that need a capability only one vendor exposes must either be modelled
on the shared interface for all transports or be declined. This has already
foreclosed per-blob access tiering.

## Constraints

- FORBID_DEPENDENCY: smblibrary
- FORBID_DEPENDENCY: blobserviceclient
- FORBID_DEPENDENCY: blobcontainerclient
