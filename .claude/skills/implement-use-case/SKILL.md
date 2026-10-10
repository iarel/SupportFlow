---
name: implement-use-case
description: Implement a SupportFlow feature or use case end to end (domain → application → infrastructure → endpoint/handler → tests). Use when asked to implement a functional requirement (FR-xxx), a command or query, an endpoint, or behavior of an aggregate such as opening a conversation, posting a message, claiming, assigning, changing status/category/priority, or AI suggestions.
---

# Implement a use case

## 1. Find the specification

- Requirement: `docs/requiremenets.md` (FR number, NFR targets and limits).
- Aggregate, commands, events and invariants: `docs/domain-model.md` (§3–§9, invariants C1–C9, A1–A5,
  state machine §3.4, consistency per operation §11).
- Flow and API conventions: `docs/architecture/components.md` (sequence diagrams §3, conventions §1.3).
- Every ADR the use case touches (`docs/desicions/`), especially 0003–0009.

Write down which module owns the use case and which other modules it reads from.

## 2. Check that the specification is final

Stop and ask the user before writing code if the use case depends on:
- an `[Assumption]` or an open question (`domain-model.md` §13);
- a behavior the documents do not describe;
- a conflict between documents, or between documents and code.

If the answer requires a new or changed decision, use the `propose-adr` skill.

## 3. Implement inside out

Work in the owning module, following `docs/architecture/architecture.md`:

1. **Domain** — aggregate method that enforces the invariants and raises domain events. No framework types.
2. **Application** — command/query handler: loads the aggregate through a port, calls it, saves in one
   transaction. Cross-module reads only through the other module's `Contracts`.
3. **Infrastructure** — port implementations in the module's own schema. Concurrency mechanism exactly as
   the ADR prescribes (`Version` + `If-Match`, claim, `SELECT … FOR UPDATE` for messages).
4. **Contracts** — integration events or query interfaces other modules need.
5. **Endpoints / EventHandlers** — map input to the command and results to HTTP codes from
   `components.md` §1.3. No business logic. Lists are paginated.
6. Integration events are written to the outbox in the same transaction; handlers are idempotent.

Keep the change to what the use case needs.

## 4. Test and verify

- Unit tests for the aggregate invariants and state transitions, including the failing cases.
- Tests for the concurrency and idempotency behavior the ADR requires, where testable.
- `dotnet build SupportFlow.slnx` and `dotnet test --solution SupportFlow.slnx` must pass with no warnings.

## 5. Report

List what was implemented, which invariants and ADRs it relies on, which tests cover it, and any
assumptions or open questions left for the user.
