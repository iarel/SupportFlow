# CLAUDE.md

## Project

SupportFlow is a customer support platform: customers open conversations, agents handle them, supervisors
manage assignment and workload, AI produces suggestions that a human accepts or rejects.

Stage 1 — modular monolith: two host processes (API and Worker), one PostgreSQL. Implemented so far: opening a
conversation end to end (`POST /conversations`) and reading it by its customer (`GET /conversations/{id}`,
`GET /conversations/{id}/messages`). Identity has `UserAccount` with JIT creation of customers
(ADR-0016); SupportOrganization has only a minimal `Team` and the seeded default team behind `ITeamQueries` (Q2 in
`domain-model.md` §13 is still open). The Worker delivers outbox events; Audit records `ConversationOpened`. The
other modules are empty skeletons; there are no AI suggestions, notifications, staff accounts or IdP choice yet.
Do not assume they exist.

## Documentation

- `docs/requiremenets.md` — requirements, load model, MVP scope
- `docs/domain-model.md` — bounded contexts, aggregates, invariants, status state machine
- `docs/architecture/` — C4 (`context`, `containers`, `components`) and module internals (`architecture.md`)
- `docs/desicions/` — ADRs; template and process in `README.md`

`requiremenets`, `desicions`, `perfomance` are misspelled; keep the names, documents link to them.
All documentation and ADRs are written in Russian.

Read the relevant documents before design decisions. Items marked `[Assumption]`, open questions in
`domain-model.md` §13 and `Proposed` ADRs are not final: if a task depends on one, say so. When requirements,
documentation and code disagree, point out the conflict and propose a resolution instead of choosing silently.
Significant decisions are recorded as ADRs. Do not create or change an ADR until the decision is agreed with
the user: propose it first.

## Commands

```bash
dotnet build SupportFlow.slnx
dotnet test --solution SupportFlow.slnx
dotnet test --solution SupportFlow.slnx --filter-class "SupportFlow.ArchitectureTests.DomainArchitectureTests"
dotnet test --solution SupportFlow.slnx --filter-method "*ShouldNotDependOnInternalsOfOtherModules"
dotnet run --project src/SupportFlow.Api        # http://localhost:5219, Development applies migrations
dotnet user-jwts create --project src/SupportFlow.Api   # development Bearer token, no IdP needed
dotnet run --project src/SupportFlow.Worker     # http://localhost:5220, health endpoints only
docker run -d -p 3000:3000 -p 4317:4317 -p 4318:4318 grafana/otel-lgtm   # then OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet ef migrations add <Name> --project src/Modules/<Module>/SupportFlow.Modules.<Module> --output-dir Infrastructure/Migrations
```

`dotnet test` needs `--solution`: the test runner is Microsoft.Testing.Platform (`global.json`).
`*.IntegrationTests` start PostgreSQL with Testcontainers and need a running Docker. The API reads
`ConnectionStrings:SupportFlow` (Development: local PostgreSQL in `appsettings.Development.json`).
`TreatWarningsAsErrors` is on, so any analyzer or style warning fails the build. CI runs build and tests in
`Release`.

## Structure

```
src/SupportFlow.Api/, src/SupportFlow.Worker/       # hosts
src/SupportFlow.ServiceDefaults/                    # OpenTelemetry and health checks of both hosts
src/BuildingBlocks/SupportFlow.BuildingBlocks/
src/Modules/<Module>/SupportFlow.Modules.<Module>/            # Domain, Application, Infrastructure, Endpoints, EventHandlers
src/Modules/<Module>/SupportFlow.Modules.<Module>.Contracts/  # public contract
tests/SupportFlow.ArchitectureTests/
tests/SupportFlow.<Project>.Tests/                  # unit tests, in-memory ports
tests/SupportFlow.Modules.<Module>.IntegrationTests/ # real PostgreSQL
tests/SupportFlow.Api.IntegrationTests/              # HTTP through the API host, test-signed JWTs
tests/SupportFlow.Worker.IntegrationTests/           # outbox delivery and cleanup across modules
```

HTTP: endpoints get the caller through `ICurrentUser` (BuildingBlocks port, implemented by Identity) and require an
Identity policy from `IdentityPolicies` (`Identity.Contracts`). Errors shared by all commands are mapped by
`ApplicationExceptionHandler` in the API host (`AccessDeniedException` → 403, `IdempotencyKeyReusedException` → 422,
`RateLimitExceededException` → 429 with `Retry-After`, `LockTimeoutException` → 503, `DomainException` → 409);
endpoints map command-specific meanings themselves. Every unit of work sets `lock_timeout` = 5 s.

Observability (containers.md): both hosts call `AddServiceDefaults()` and `MapHealthEndpoints()`
(`/health/live`, `/health/ready`). OpenTelemetry is configured by `OTEL_*` variables and exports only when
`OTEL_EXPORTER_OTLP_ENDPOINT` is set. SupportFlow activity sources and meters are named `SupportFlow.*`. Outbox rows
carry the publishing request's `traceparent`, so delivery continues its trace; `IntegrationEventContext.CorrelationId`
is the trace id. Target platform is Kubernetes (ADR pending); the production IdP is still open (ADR-0011).

Persistence: one `DbContext` per module with its own schema, snake_case names, migrations and
`__ef_migrations_history` table; the module's `idempotency_keys`, `outbox` and `inbox` tables come from
`ModelBuilderExtensions` in BuildingBlocks. Generated migrations are marked as generated code in `.editorconfig`.
`IUnitOfWork`, `IIdempotencyStore`, `IIntegrationEventOutbox` and `IInbox` are bound to a module's `DbContext`, so
they are not registered in the shared DI container: the module's `Add<Module>()` builds its handlers with its own
instances.

Events (ADR-0003, ADR-0008, ADR-0015): a publishing module registers `AddOutbox<TContext>(<its event names>)`; a
consuming module implements `IIntegrationEventHandler<TEvent>` in `EventHandlers/` with a stable `Name`, registers
it with `AddIntegrationEventHandler`, and its Application handler writes `IInbox` first in the same unit of work.
Modules with these tables also call `AddRetentionCleanup<TContext>()`. The Worker runs the dispatcher and cleanup.

Modules: `Conversations`, `SupportOrganization`, `Identity`, `AIAssistance`, `Notifications`, `Audit`,
`Reporting`. A module has only the layer folders it needs (`architecture.md` §2).

Adding a module or project requires all of:
- registration in `SupportFlow.slnx` (XML `<Folder>`/`<Project>`, not a classic `.sln`);
- for a module: `Add<Module>()` in both `Program.cs`, `Map<Module>Endpoints()` in the API if it has endpoints;
- for a module: its name in `Modules.Names` (`tests/SupportFlow.ArchitectureTests/Modules.cs`) and both of its
  projects in the test project references — otherwise architecture tests silently skip it.

Package versions go to `Directory.Packages.props`, not `.csproj`. Shared build settings live in
`Directory.Build.props`; do not repeat them per project. Files use LF; there is no `.gitattributes`.

## Architecture rules

From `architecture.md` §15, ADR-0002, ADR-0013:

1. Domain does not depend on Application, Infrastructure, ASP.NET Core, EF Core.
2. Application depends on ports, not on Infrastructure; endpoints contain no business logic.
3. A module depends only on another module's `*.Contracts`; `Contracts` depend only on the BCL.
4. A module uses only its own DB schema. Exception: read-only `reporting` views (ADR-0010).
5. Cross-module synchronous calls are reads; changes go through integration events and the outbox (ADR-0003).
   Integration events are plain records in `Contracts` with no base type; the outbox assigns `EventId`, and
   domain events are mapped to them explicitly in Application (ADR-0015).
6. Invariants live in aggregates, not in SQL conditions. One aggregate per transaction, except registering a
   message: sending, and the first message on open (ADR-0004).
7. Deviations require an ADR agreed with the user.

Rules 1–3 are partly checked by the architecture tests.

Invariants that must hold everywhere (details in the ADRs):
- AI never changes conversation state; it only creates `AISuggestion` (ADR-0009).
- Message order is defined only by per-conversation `Seq` (ADR-0005).
- Concurrency: claim (ADR-0006), `If-Match`/`Version` (ADR-0007), idempotency keys (ADR-0014; event handlers — ADR-0008),
  lease-based jobs (ADR-0003). API error codes and pagination: `components.md` §1.3.
- No Kafka, Redis, separate databases or microservices without a measured reason (requirements §13).

Use the domain terms from `domain-model.md` §1: Conversation (not Ticket), Message, Seq, Claim, Assign.

## Naming

- Module projects: `SupportFlow.Modules.<Module>`, contracts: `SupportFlow.Modules.<Module>.Contracts`.
- Namespaces follow folders: `SupportFlow.Modules.<Module>.Domain`, `.Application`, `.Infrastructure`,
  `.Endpoints`, `.EventHandlers`, `.Contracts`. Never `SupportFlow.<Module>.*`.
- No `Module` suffix on module names or types.
- Test names describe the rule or behavior: `DomainArchitectureTests.ShouldNotDependOnOuterLayersOrFrameworks`.

Standard C# casing, `I` prefix, `_camelCase` private fields and no underscores in method names are enforced by
the build (`.editorconfig`, CA1707).

## Open questions

- `BuildingBlocks/Persistence/` (EF unit of work, idempotency store, model extensions) is not described in the
  documentation (`components.md` lists outbox, inbox, unit of work and domain base types).
- How migrations are applied at deployment (on startup or as a separate step) is not decided.
