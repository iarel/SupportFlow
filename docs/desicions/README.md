# Architecture Decision Records

Каждое существенное архитектурное решение фиксируется по схеме из требований (§16):
**Problem → Constraints → Options → Decision → Trade-offs**.

ADR не редактируются задним числом. Если решение меняется, создаётся новый ADR, а старый получает статус
`Superseded by ADR-XXXX`. Так история ADR показывает эволюцию архитектуры.

## Статусы

`Proposed` → `Accepted` → (`Superseded by ADR-XXXX` | `Deprecated`)

## Реестр

| ADR | Решение | Stage | Статус |
|---|---|---|---|
| [0001](0001-modular-monolith-api-and-worker.md) | Модульный монолит: API + Worker, одна PostgreSQL | 1 | Proposed |
| [0002](0002-module-boundaries.md) | Границы модулей: schema per module, зависимость только от Contracts | 1 | Proposed |
| [0003](0003-transactional-outbox-postgresql-queue.md) | Transactional outbox и очередь на PostgreSQL | 1 | Proposed |
| [0004](0004-conversation-and-message-aggregates.md) | Conversation и Message — отдельные агрегаты | 1 | Proposed |
| [0005](0005-message-ordering-seq.md) | Порядок сообщений через per-conversation `Seq` | 1 | Proposed |
| [0006](0006-claim-via-conditional-update.md) | Текущий Assignee в Conversation, claim условным UPDATE по `Version` | 1 | Proposed |
| [0007](0007-optimistic-concurrency.md) | Optimistic concurrency: `Version` + ETag | 1 | Proposed |
| [0008](0008-idempotency.md) | Идемпотентность записи и обработки событий | 1 | Proposed |
| [0009](0009-ai-assistance-separate-context.md) | AI Assistance — отдельный контекст, AI не изменяет домен | 1 | Proposed |
| [0010](0010-reporting-read-only-views.md) | Reporting через read-only views | 1 | Proposed |
| [0011](0011-external-identity-provider.md) | Внешний Identity Provider, JWT, stateless API | 1 | Proposed |
| [0012](0012-rate-limiting.md) | Стратегия rate limiting | 1 | Proposed |
| [0013](0013-module-internal-structure.md) | Внутреннее устройство модуля: DDD, Onion/Clean, Ports & Adapters | 1 | Proposed |
| [0014](0014-idempotency-keys-table.md) | Ключи идемпотентности HTTP-команд в отдельной таблице модуля | 1 | Proposed |
| [0015](0015-integration-event-contracts.md) | Integration events — records в `Contracts`, `EventId` присваивает outbox | 1 | Proposed |
| [0016](0016-user-account-mapping-jit-customer.md) | `UserAccount` по `(issuer, sub)`, JIT-создание Customer в модуле Identity | 1 | Proposed |

## Шаблон

```markdown
# ADR-XXXX: <Название>

- **Status:** Proposed
- **Date:** YYYY-MM-DD
- **Stage:** N
- **Related:** ADR-…, requirements §…

## Problem
## Constraints
## Options
## Decision
## Trade-offs
```
