# BackupHelper — Copilot Instructions

## Purpose

BackupHelper is a .NET 9 backup utility that zips files/directories from local and SMB sources and uploads archives to local filesystem and Azure Blob sinks. Behavior is driven by JSON backup plans and MediatR-based workflows.

Keep changes aligned with the current solution layout, DI wiring, and extension patterns in this repository.

## Current Solution Scope

- Treat projects listed in BackupHelper.sln as the active architecture.
- Do not edit generated artifacts (`bin/`, `obj/`) or user-local files (`*.csproj.user`, `*.sln.DotSettings.user`) unless explicitly requested.

## Layering Rules

```
ConsoleApp (wizard interaction and app entry)
   -> Api (MediatR handlers for app-level operations)
   -> Core (domain orchestration, managers, zip pipeline)
   <-> Abstractions (contracts)
   <-> Sources.* / Sinks.* / Connectors.* (implementations)
```

- Keep interactive flow and prompts in ConsoleApp wizard steps.
- Keep orchestration logic in Core services/managers.
- Keep contracts in Abstractions and implementation details in feature projects.
- Extend existing managers and factories before introducing parallel patterns.

## Dependency Injection Conventions

- Register services in layer ConfigureServices classes:
   - AddCoreServices(IConfiguration configuration)
   - AddApiServices(IConfiguration configuration)
   - AddConsoleInterfaceServices(IConfiguration configuration)
- Follow startup composition in src/BackupHelper.ConsoleApp/Program.cs.
- Prefer constructor injection and avoid ad-hoc service creation.
- When adding new source/sink/credential types, ensure DI registration in Core ConfigureServices.

## Search and Analysis Hygiene

- Prefer code searches scoped to `src/` and `tests/` to avoid false positives from generated output under `bin/` and `obj/`.
- Treat generated publish artifacts and obj snapshots as non-authoritative during reviews.

## Backup Plan and Serialization

- Backup plan types and converters live in src/BackupHelper.Core/BackupZipping/BackupPlan.cs.
- Use Newtonsoft.Json and [JsonProperty] attributes for plan serialization.
- Keep polymorphic conversion logic in:
   - BackupEntryConverter.ReadJson()
   - SinkDestinationConverter.ReadJson()
- When adding new backup entry or sink destination types, update converter switch/dispatch logic in this file.
- Preserve current BackupPlan options and defaults:
   - Items, Sinks, LogDirectory
   - ThreadLimit, MemoryLimitMB, CompressionLevel, ZipFileNameSuffix

## Sources

- Source implementations must satisfy ISource.
- SourceManager resolves by scheme prefix and falls back to FileSystemSource when no scheme exists.
- To add a source:
1. Implement ISource in BackupHelper.Sources.{Name}.
2. Register it in Core ConfigureServices.AddSources().
3. Keep scheme behavior compatible with SourceManager path parsing.

File-in-use handling:

- FileSystemSource first attempts normal file access.
- On locked-file failures, fallback goes through IFileInUseSourceManager.
- VSS path is provided by VssFileInUseSourceFactory and VssFileInUseSource.

## Sinks

- Sinks use ISink + ISinkFactory and are selected by destination Kind.
- Follow existing base patterns:
   - SinkBase<TDestination>
   - SinkFactoryBase<TSink, TDestination>
- To add a sink:
1. Create {Name}SinkDestination : ISinkDestination with const string SinkKind.
2. Create {Name}Sink : SinkBase<{Name}SinkDestination>.
3. Create {Name}SinkFactory : SinkFactoryBase<{Name}Sink, {Name}SinkDestination>.
4. Register factory in Core ConfigureServices.AddSinks().
5. Update SinkDestinationConverter.ReadJson() in BackupPlan.cs.

## Credentials and Security

- Credentials are managed via ICredentialsProvider and typed ICredentialHandler<T> handlers.
- CredentialHandlerRegistry maps runtime credential kind to handler.
- Keep title metadata compatible with NameValuePairHelper parsing.
- Handle passwords, SAS values, and other secrets as SensitiveString rather than plain string when moving through domain and connector layers.
- Prefer SensitiveString constructors that avoid unnecessary immutable string retention, and dispose SensitiveString instances when ownership ends.
- Be explicit about SensitiveString ownership boundaries: if constructors or records clone incoming SensitiveString values, callers must still dispose their original instances.
- Avoid inline temporary secret construction when passing into cloning constructors (especially inside LINQ projections); use a local `using var` SensitiveString first.
- Do not log or expose sensitive data such as passwords, tokens, SAS values, or full connection strings.
- Preserve secure handling patterns for sensitive buffers and disposable secrets.

## Logging and Runtime Pitfalls

- Do not repeatedly mutate a shared ILoggerFactory per backup run by adding new providers each time.
- In wizard-driven repeated backups, repeated AddSerilog on the shared factory can accumulate providers and duplicate logs across runs.
- Prefer per-run isolated logging scopes/providers or one-time provider registration with contextual routing (for example LogContext properties and sink-side filtering).
- Current mitigation lives in `PerformBackupStep.AddBackupLogSink`: it uses a `BackupLogDirectory` LogContext property and one-time registration per normalized directory. Preserve this property/filter coupling when changing log routing.

## MediatR and Wizard Patterns

- API features under src/BackupHelper.Api/Features use IRequest/IRequestHandler.
- Wizard flow uses IWizardParameters and IWizardStep<TParameters>.
- Program.cs runs the wizard loop asynchronously by repeatedly awaiting mediator.Send(parameters) until null.
- In wizard and API handlers, prefer async end-to-end flow; avoid blocking calls (`.Result`, `.Wait()`) and pass cancellation tokens through async boundaries.
- Prefer adding new user flows as wizard steps rather than embedding logic in Program.cs.

## Zip Pipeline Notes

Core zip abstractions are in src/BackupHelper.Core/FileZipping/.

- InMemoryFileZipper: memory-backed archive, requires SaveAsync(), single-threaded behavior.
- OnDiskFileZipper: stream-to-disk implementation, production default factory.
- Parallel compression uses ZipTaskQueue with thread and memory budgets.
- BackupPlanZipper orchestrates recursive item processing and two-phase encrypted output behavior.

## Testing Conventions

- Test framework is NUnit (NUnit 4 packages in test projects).
- Prefer test names in Given_When_Then style.
- Shared infrastructure is in tests/BackupHelper.Tests.Shared/TestsBase.cs.
- TestsBase composes services with AddCoreServices/AddApiServices and user-secrets-backed test configuration.
- Integration tests for SMB/VSS may require environment setup and secrets.
- For implementation changes, run targeted tests for touched projects first, then broaden to full suite as needed.

## Build and Run

```bash
dotnet build
dotnet test
dotnet run --project src/BackupHelper.ConsoleApp
```

## Copilot Change Checklist

Before finalizing changes, try to verify:

1. DI registrations are updated in the appropriate ConfigureServices extension.
2. New polymorphic backup plan types are reflected in BackupPlan converters.
3. New source/sink implementations follow existing manager/factory patterns.
4. Sensitive values are not added to logs, exceptions, or serialized output.
5. Tests are added or updated using repository conventions.
6. SensitiveString ownership/disposal is clear at call sites and constructors (no temporary-secret leaks).
7. Logging configuration does not accumulate duplicate providers across repeated backup runs.
8. Generated artifacts and user-local files were not edited unless explicitly requested.
9. Async updates remain end-to-end (awaited mediator and I/O calls, with cancellation token propagation where available).
