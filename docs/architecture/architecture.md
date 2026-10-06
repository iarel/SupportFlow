# SupportFlow — Внутренняя архитектура модулей

> **Архитектурный этап: Stage 1 — Naive modular monolith.**
> Документ задаёт правила внутреннего устройства модулей. Процессы, хранилища и взаимодействие с внешними
> системами описаны в C4-документах. Решение зафиксировано в
> [ADR-0013](../desicions/0013-module-internal-structure.md).
>
> Связанные документы: [context.md](context.md) · [containers.md](containers.md) ·
> [components.md](components.md) · [domain-model.md](../domain-model.md) ·
> [ADR-0002](../desicions/0002-module-boundaries.md)

---

## 1. Архитектурный стиль

SupportFlow — **модульный монолит**, спроектированный по принципам **Domain-Driven Design**. Каждый bounded
context из [domain-model](../domain-model.md) реализован как изолированный модуль.

Внутри модуля применяются:

- **Onion Architecture** — зависимости направлены к домену;
- **Clean Architecture** — разделение domain, application и infrastructure;
- **Hexagonal Architecture (Ports & Adapters)** — взаимодействие с внешним миром через явные порты и
  адаптеры.

Это не три независимых набора слоёв, а три взгляда на одно устройство:

- DDD определяет, как моделируется предметная область;
- Onion/Clean определяют направление зависимостей и разделение ответственности;
- Hexagonal определяет, как приложение взаимодействует с внешним миром.

---

## 2. Модули

Модули соответствуют bounded contexts ([domain-model §2](../domain-model.md),
[ADR-0002](../desicions/0002-module-boundaries.md)). Не у каждого модуля есть все слои: слой появляется,
только если в нём есть содержание.

| Модуль | Domain | Inbound adapters | Комментарий |
|---|---|---|---|
| **Conversations** (Core) | `Conversation`, `Message`, `Category` | HTTP, Scheduler | Публикует read-only views для Reporting (ADR-0010) |
| **Support Organization** | `Team`, `StaffMember` | HTTP | Источник истины для ролей сотрудников |
| **Identity** | `UserAccount`, `CustomerProfile` | HTTP | Сопоставление IdP `sub` → `UserAccount` |
| **AI Assistance** | `AISuggestion` | HTTP, event handlers, AI Job Processor | Вызывает AI Provider через ACL |
| **Notifications** | `Notification` | Event handlers, Notification Sender | Нет HTTP-операций, работает только в Worker |
| **Audit** | `AuditEvent` | HTTP (чтение), event handlers | Append-only |
| **Reporting** | — | HTTP (чтение) | **Нет Domain-слоя.** Только запросы к схеме `reporting` |

**Граница модуля — главная архитектурная граница.** Слои существуют внутри модулей и не должны
превращаться в глобальную структуру `Controllers → Services → Repositories`.

---

## 3. Структура модуля

```text
src/Modules/Conversations/
├── SupportFlow.Conversations/              # внутренняя реализация модуля (internal)
│   ├── Domain/                             # агрегаты, value objects, domain events
│   ├── Application/                        # use cases (commands / queries), порты
│   ├── Infrastructure/                     # outbound adapters: EF Core, репозитории, SQL, ACL-клиенты
│   ├── Endpoints/                          # inbound adapter: HTTP route group (Api host)
│   └── EventHandlers/                      # inbound adapter: integration event handlers, jobs (Worker host)
└── SupportFlow.Conversations.Contracts/    # публичный контракт модуля
                                            # integration events, query interfaces, DTO
```

- **Слои — это папки (namespaces) внутри одного проекта модуля, а не отдельные проекты.** Разбиение каждого
  модуля на 4+ проекта отвергнуто в [ADR-0002](../desicions/0002-module-boundaries.md).
- **`Contracts` — отдельный проект.** Так правило «другие модули зависят только от Contracts» проверяет
  компилятор.
- Направление зависимостей между слоями внутри модуля проверяется архитектурными тестами
  (`tests/SupportFlow.ArchitectureTests`).
- Hosts (`SupportFlow.Api`, `SupportFlow.Worker`) только компонуют модули: Api регистрирует `Endpoints`,
  Worker — `EventHandlers`, job processors и scheduled jobs.

Соответствие терминов Hexagonal Architecture: **inbound adapters** — `Endpoints`, `EventHandlers`, job
processors и scheduled jobs; **outbound adapters** — `Infrastructure`.

---

## 4. Направление зависимостей

```text
Endpoints / EventHandlers / Jobs        Infrastructure
            │                                 │
            └──────────────┬──────────────────┘
                           ▼
                      Application  ───────▶  Contracts других модулей
                           │
                           ▼
                        Domain
```

- Domain не зависит от Application, Infrastructure, ASP.NET Core, EF Core, PostgreSQL, внешних API и других
  технологий.
- Application не зависит от конкретных реализаций инфраструктуры. Он объявляет порты, которые реализует
  Infrastructure.
- Зависимость на другие модули допускается только из Application и только на их `Contracts`.
- `Contracts` не зависят ни от чего, кроме BCL.

---

## 5. Domain

Domain описывает бизнес-понятия и бизнес-правила. Он может содержать агрегаты, сущности, value objects,
domain services, domain events, доменные политики и инварианты.

Domain должен содержать поведение, а не сводиться к анемичным структурам данных. Пример — агрегат
`Conversation` ([domain-model §3.2](../domain-model.md)):

```text
Conversation
 ├── Claim(agentId)
 ├── Assign(agentId) / Reassign(agentId) / Unassign()
 ├── ChangeStatus(to, actor)
 ├── ChangeCategory(categoryId, basedOnSuggestionId?)
 ├── ChangePriority(priority, basedOnSuggestionId?)
 └── RegisterMessage(author) → Seq        # Message — отдельный агрегат (ADR-0004)
```

Инварианты агрегата (C1–C9 для `Conversation`) проверяет сам агрегат.

Domain не содержит: HTTP, DTO, конфигурацию EF Core, SQL, клиенты внешних API, реализации логирования и
трассировки, инфраструктуру фреймворков.

---

## 6. Application

Application реализует use cases. Примеры:

```text
OpenConversation
PostMessage
ClaimConversation
AssignConversation
ChangeConversationStatus
ChangeConversationPriority
RequestAISuggestion
RejectAISuggestion
```

Ответственность:

- оркестрация use case;
- загрузка нужных агрегатов через порты;
- вызов доменного поведения;
- управление транзакцией: **одна транзакция на команду, один агрегат на транзакцию**. Единственное
  исключение — `PostMessage` ([ADR-0004](../desicions/0004-conversation-and-message-aggregates.md));
- проверки, которым нужны данные других модулей (например, «Agent активен и состоит в команде»), через их
  `Contracts`;
- проверки принадлежности (Customer владеет обращением, Supervisor отвечает за команду обращения);
- формирование результата.

Application не содержит бизнес-правил, принадлежащих Domain.

**CQS.** Команды проходят через агрегаты. Запросы читают данные напрямую (SQL или EF projection в
Infrastructure), минуя доменную модель. Отдельной БД или отдельного сервиса для чтения нет.

---

## 7. Порты и адаптеры

### Inbound adapters

```text
HTTP endpoints              (Api)
Integration event handlers  (Worker)
Job processors              (Worker)  — AI jobs, отправка уведомлений
Scheduled jobs              (Worker)  — авто-закрытие, retention, очистка outbox/inbox
```

Они переводят внешний вход в команды и запросы Application.

### Outbound ports

```text
IConversationRepository
IUnitOfWork
IIntegrationEventOutbox
IAIProvider
INotificationSender
IClock
ICurrentUser
```

### Outbound adapters

```text
PostgreSQL / EF Core
AI Provider adapter            (ACL, ADR-0009)
Notification Provider adapter  (ACL)
```

Application зависит от порта, а не от адаптера:

```text
Application ──▶ Port ◀── Adapter
```

Аутентификация (проверка JWT через JWKS у Identity Provider) — часть HTTP pipeline Api host, а не outbound
порт. Application получает текущего пользователя через `ICurrentUser`
([ADR-0011](../desicions/0011-external-identity-provider.md)).

---

## 8. Границы модулей

Правила [ADR-0002](../desicions/0002-module-boundaries.md):

1. Модуль зависит **только от `*.Contracts`** другого модуля.
2. Модуль читает и пишет **только свою схему БД**, без cross-schema FK и JOIN. Исключение — read-only views
   в схеме `reporting` ([ADR-0010](../desicions/0010-reporting-read-only-views.md)).
3. Синхронные межмодульные вызовы допускаются **только для чтения**. Изменения в другом модуле происходят
   только через integration events ([ADR-0003](../desicions/0003-transactional-outbox-postgresql-queue.md)).
4. Правила проверяются архитектурными тестами и компилятором.

```text
Conversations ──▶ SupportOrganization.Contracts       ✔
Conversations ──▶ SupportOrganization (internal)      ✘
Conversations ──▶ organization.* (таблицы)            ✘
```

Модульная структура должна делать возможным выделение модуля в отдельный сервис, но **выделение сервисов
не является текущим требованием**.

---

## 9. Persistence

- PostgreSQL — основное хранилище. **Схема на модуль** (`conversations`, `organization`, `identity`, `ai`,
  `notifications`, `audit`, `reporting`).
- Domain не знает о persistence.
- Infrastructure содержит EF Core mappings, DbContext модуля, репозитории, SQL-запросы и, при
  необходимости, persistence-модели.
- Read side (запросы, отчёты) может обходить доменную модель.
- Ограничения уникальности, обеспечивающие идемпотентность (`IdempotencyKey`, inbox, `SourceEventId`),
  принадлежат схеме модуля-владельца ([ADR-0008](../desicions/0008-idempotency.md)).

---

## 10. Инварианты и конкурентный доступ

Инварианты проверяет агрегат. БД обеспечивает только атомарность сохранения и обнаружение конфликта. Бизнес-
правила не переносятся в SQL-условия.

| Операция | Как сохраняется | Конфликт |
|---|---|---|
| Изменение `Conversation` по запросу пользователя (статус, категория, приоритет, assign) | Загрузка → метод агрегата → `UPDATE … WHERE id = @id AND version = @expected`, где `@expected` берётся из `If-Match` | 0 строк → `412` ([ADR-0007](../desicions/0007-optimistic-concurrency.md)) |
| Claim | Загрузка → `Conversation.Claim(agentId)` → тот же UPDATE, `@expected` — загруженная версия | 0 строк → `409` ([ADR-0006](../desicions/0006-claim-via-conditional-update.md)) |
| Отправка сообщения | `SELECT … FOR UPDATE` строки `Conversation` → `RegisterMessage(author)` → UPDATE + INSERT `Message` | Сериализуется блокировкой строки ([ADR-0004](../desicions/0004-conversation-and-message-aggregates.md), [ADR-0005](../desicions/0005-message-ordering-seq.md)) |

---

## 11. Сквозная функциональность

Сквозная функциональность не проникает в Domain и не заменяет бизнес-правила.

| Concern | Где реализуется |
|---|---|
| Authentication | Api host: JWT Bearer pipeline ([ADR-0011](../desicions/0011-external-identity-provider.md)) |
| Authorization по роли | Endpoints: authorization policies |
| Authorization по принадлежности | Application: нужны данные обращения и команд |
| Rate limiting, короткие окна | Api host: in-memory `RateLimiter` ([ADR-0012](../desicions/0012-rate-limiting.md)) |
| Rate limiting, суточный лимит обращений | Application, в транзакции создания обращения ([ADR-0012](../desicions/0012-rate-limiting.md)) |
| Idempotency | Endpoints читают `Idempotency-Key`. Дубли отсекает ограничение уникальности в схеме модуля, в обработчиках событий — inbox ([ADR-0008](../desicions/0008-idempotency.md)) |
| Optimistic concurrency | Endpoints читают `If-Match`. Проверка — при сохранении агрегата ([ADR-0007](../desicions/0007-optimistic-concurrency.md)) |
| Validation | Endpoints — формат входа. Domain — инварианты (например, `MessageBody` ≤ 10 KB) |
| Logging, tracing, metrics, correlation ID | Hosts и Infrastructure, OpenTelemetry |
| Resilience (timeout, retry, bulkhead) | Outbound adapters и job processors в Worker |

---

## 12. API

API — inbound adapter. Его ответственность ограничена:

- обработка HTTP;
- интеграция с authentication и authorization;
- валидация формата запроса;
- отображение HTTP-запросов в команды и запросы Application;
- отображение результатов и ошибок в HTTP-ответы (`409`, `412`, `428`, `429` и т. д., см.
  [components.md §1.3](components.md)).

API не содержит бизнес-логики. Endpoints не работают с репозиториями и DbContext напрямую.

---

## 13. Фоновая обработка

Долгая и асинхронная работа выполняется в отдельном host-процессе Worker
([ADR-0001](../desicions/0001-modular-monolith-api-and-worker.md)): доставка outbox, AI jobs, уведомления,
периодические задачи.

Обработчики и jobs — inbound adapters. Они вызывают use cases Application и не дублируют бизнес-логику.
Например, авто-закрытие `Resolved` вызывает `Conversation.ChangeStatus(Closed, System)`, а не выполняет
`UPDATE` в обход агрегата.

---

## 14. Транзакции, domain events и integration events

- **Domain event** — внутреннее событие модуля, порождается агрегатом. Обрабатывается внутри модуля в той же
  транзакции, если вообще требует обработки.
- **Integration event** — публичное событие модуля, объявлено в `Contracts`. Пишется в outbox **в той же
  транзакции**, что и изменение агрегата, и доставляется Worker'ом at-least-once
  ([ADR-0003](../desicions/0003-transactional-outbox-postgresql-queue.md)). Обработчики идемпотентны за счёт
  inbox.
- Dual write запрещён: код не публикует сообщения во внешние системы или брокер после коммита и не пишет в БД
  после публикации.

---

## 15. Архитектурные правила

Обязательные правила:

1. Domain не зависит от инфраструктуры.
2. Domain не зависит от ASP.NET Core и EF Core.
3. Application не зависит от конкретных реализаций инфраструктуры.
4. Infrastructure реализует порты, объявленные внутренними слоями.
5. API не содержит бизнес-логики.
6. Модуль зависит только от `Contracts` других модулей.
7. Модуль не обращается к таблицам другого модуля. Исключение — views в схеме `reporting` (ADR-0010).
8. Синхронные межмодульные вызовы — только чтение. Изменения — только через integration events.
9. Бизнес-инварианты принадлежат Domain.
10. Application оркестрирует use cases и не реализует доменные правила.
11. Внешние интеграции доступны только через порты.
12. Сквозная функциональность не проникает в Domain.
13. Каждое отступление от правил требует ADR. Действующие исключения: отправка сообщения изменяет два
    агрегата в одной транзакции (ADR-0004); Reporting читает views, опубликованные другими модулями
    (ADR-0010).

Правила 1–8 проверяются архитектурными тестами или компилятором.

---

## 16. Прагматизм

Архитектура служит сопровождаемости, моделированию домена, тестируемости и эволюции системы.

Паттерны не вводятся ради терминологии. Для простых операций предпочтительны простые реализации, если они
не нарушают:

- инварианты домена;
- границы модулей;
- правила зависимостей;
- тестируемость;
- долгосрочную сопровождаемость.

Архитектура оптимизируется на **ясные границы и явные зависимости**, а не на максимальное число
абстракций. Например, Reporting не имеет Domain-слоя, а простые запросы не проходят через агрегаты.

---

## 17. Эволюция

SupportFlow начинается как модульный монолит. Границы модулей явные, поэтому отдельный модуль можно
выделить в сервис, если этого потребуют масштабирование, организация команд или эксплуатация (§12.1).

Распределённое развёртывание само по себе не является целью:

> **Сначала модульность; распределение — только при конкретной причине.**
