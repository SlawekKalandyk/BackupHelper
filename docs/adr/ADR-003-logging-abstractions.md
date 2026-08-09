---
id: ADR-003
title: Libraries log through ILogger and concrete logger wiring lives at the composition root
status: accepted
priority: foundational
date: 2026-08-07
scope: core.sinks.sources.abstractions.logging
supersedes: []
---

## Context

The wizard writes human-readable output to the terminal and, when a log
directory is configured, rolling files. That rendering choice belongs to the
executable, not to the libraries that produce the events.

The quality attribute at stake is **reusability**, with a **deployability**
driver behind it: the library assemblies are candidates for reuse by a
scheduled service or a future API host, and a library that hard-references a
specific logging implementation drags that implementation and its sinks
configuration into every host that consumes it.

## Decision

Library assemblies depend only on the Microsoft.Extensions.Logging
abstractions and log through ILogger. The concrete logger, its enrichers, and
its output sinks are configured once, at the composition root of the
executable.

## Consequences

Structured-logging features specific to one implementation are unavailable
inside the libraries. Where enrichment matters, the context must be passed as
ILogger scope state instead.

Any future host must configure a logger, or library log events go nowhere.

## Constraints

- FORBID_DEPENDENCY: serilog
