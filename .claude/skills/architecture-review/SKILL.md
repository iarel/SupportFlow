---
name: architecture-review
description: Review SupportFlow code changes against the architecture rules and ADRs — module boundaries, layer dependencies, database schema ownership, transactions, outbox usage, domain invariants and concurrency. Use when asked for an architecture review, before finishing a non-trivial change, or when reviewing a branch, diff or pull request.
---

# Architecture review

Read-only: report findings, do not change code.

## 1. Scope

Determine the change set: the diff the user names, otherwise `git diff` against the main branch plus
uncommitted changes. Note which modules and layers are touched.

Read `docs/architecture/architecture.md` (§10 concurrency, §15 rules) and the ADRs relevant to the touched
code.

## 2. Check what the architecture tests do not

The tests in `tests/SupportFlow.ArchitectureTests` cover only part of the layer and module dependencies.
Check by reading the code:

- **Boundaries** — references to another module's internal project (even unused), access to another
  module's tables or schema, cross-schema JOIN outside `reporting` views, writes into another module
  without integration events.
- **Layers** — business logic in endpoints or handlers, endpoints using repositories or `DbContext`,
  framework types in Domain, Application depending on concrete infrastructure.
- **Invariants** — rules enforced by SQL conditions or application code instead of the aggregate; more
  than one aggregate changed in one transaction (only sending a message may do this).
- **Concurrency and reliability** — the mechanism prescribed by the ADR is used: `Version`/`If-Match`
  (ADR-0007), claim (ADR-0006), `Seq` ordering (ADR-0005), idempotency keys and inbox (ADR-0008), outbox in
  the same transaction and lease-based jobs (ADR-0003).
- **AI** — AI never changes conversation state (ADR-0009).
- **Infrastructure** — no new brokers, caches, databases or services without an agreed ADR.
- **Registration** — a new module or project is registered everywhere listed in `CLAUDE.md`.
- **Language** — domain terms from `domain-model.md` §1.

## 3. Report

For each finding: file and line, the violated rule with its source (`architecture.md` §, ADR number), why it
matters, and a suggested fix. Order by severity. Separate confirmed violations from questions where the
documentation is ambiguous. If nothing is found, say so and list what was checked.
