# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

SupportFlow is at the very earliest scaffold stage: a default ASP.NET Core minimal API template with no
real endpoints, no domain code, and no tests yet. `docs/requiremenets.md` (note the typo — that's the
actual filename) is an empty requirements outline whose section headers indicate the intended shape of the
product:

- Actors: **Customer**, **Support Agent**, **Supervisor**, **AI**, **System**
- Sections planned but not yet filled in: Product Overview, Functional/Non-Functional Requirements, Scale
  Assumptions, Consistency/Availability/Security/Observability Requirements, MVP Scope, Out of Scope.

This tells you the intended product is an AI-assisted customer support platform, but no architectural
decisions, domain model, or API surface have been committed yet. When asked to build features, check
`docs/requiremenets.md` and `docs/desicions/` (ADRs — also a typo'd directory name, currently empty) first
for any newly-added guidance before inventing your own design.

## Repository layout

```
SupportFlow.slnx              # Solution file (new .slnx format, not .sln)
src/SupportFlow.Api/          # The only project: ASP.NET Core minimal API (net10.0)
tests/                        # Empty — no test project exists yet
deploy/                       # Empty — reserved for deployment configs (Docker/K8s/etc.)
load/                         # Empty — reserved for load testing
perfomance/                   # Empty — reserved for performance testing (note the typo)
docs/architecture/            # Empty — reserved for architecture docs
docs/desicions/               # Empty — reserved for ADRs (note the typo)
docs/requiremenets.md         # Requirements outline (note the typo), currently just section headers
```

When adding new projects (a domain/infrastructure layer, a test project, etc.), register them in
`SupportFlow.slnx` following its `<Folder>`/`<Project>` XML structure — it is not a classic `.sln` file.

## Common commands

Run all commands from the repository root.

```bash
# Restore + build
dotnet build SupportFlow.slnx

# Run the API (reads Properties/launchSettings.json for URLs/env)
dotnet run --project src/SupportFlow.Api

# Hot-reload dev loop
dotnet watch --project src/SupportFlow.Api run
```

The API listens on `http://localhost:5219` (and `https://localhost:7229` under the `https` launch profile),
with `ASPNETCORE_ENVIRONMENT=Development`.

There is no test project yet, so there are no `dotnet test` targets to run. Once a test project is added
under `tests/`, add it to `SupportFlow.slnx` and document the run/single-test commands here.

## Architecture notes

- `src/SupportFlow.Api/Program.cs` currently uses the ASP.NET Core **minimal API** style (top-level
  `WebApplication` builder, `app.MapGet(...)`) rather than controller-based MVC — follow this style for new
  endpoints unless the project is deliberately restructured.
- `Nullable` and `ImplicitUsings` are both enabled in `SupportFlow.Api.csproj` — write nullable-aware code
  and don't add redundant `using` statements for BCL namespaces already covered by implicit usings.
- Target framework is `net10.0`.
