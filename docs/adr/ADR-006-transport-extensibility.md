---
id: ADR-006
title: New destinations and sources are added by implementing shared interfaces and registering the kind
status: accepted
priority: foundational
date: 2026-08-07
scope: sinks.sources.abstractions.wizard
supersedes: []
---

## Context

A plan names its destinations by a `kind` discriminator, and sources by a URI
scheme. Both sets are expected to grow — S3 and SFTP are the obvious next
destinations.

The quality attribute is **extensibility**, expressed concretely: adding a
destination should touch a new assembly and a registration line, and nothing
else. The failure mode to avoid is the discriminator leaking into orchestration
as a switch statement that every new kind has to be threaded through, which is
how the wizard, the uploader, and the retention pruner would each acquire their
own copy of the same list.

## Decision

A destination implements the shared sink interface; a source implements the
shared source interface. Concrete implementations are registered against their
discriminator through dependency injection at startup. The wizard and the
orchestrator resolve implementations by discriminator and never name a concrete
kind.

## Consequences

An unknown discriminator is a runtime failure rather than a compile-time one.
This is mitigated by validating a plan's discriminators up front and reporting
unsupported values before any archive work begins.

Capabilities that only some destinations support — retention pruning is the
current example — have to be modelled as an optional interface rather than
assumed on the base contract.
