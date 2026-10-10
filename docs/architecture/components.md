# C4 · Level 3 — Components

> **Архитектурный этап: Stage 1 — Naive modular monolith.**
>
> Связанные документы: [context.md](context.md) · [containers.md](containers.md) ·
> [architecture.md](architecture.md) · [domain-model.md](../domain-model.md)

---

## 1. SupportFlow API

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
flowchart TB
    web["<b>Web Client</b><br/>[Container]"]
    db[("<b>SupportFlow DB</b><br/>[PostgreSQL]")]
    idp["<b>Identity Provider</b>"]

    subgraph api["SupportFlow API [Container]"]
        direction TB

        subgraph pipeline["HTTP Pipeline (cross-cutting)"]
            authn["<b>Authentication</b><br/>JWT Bearer"]
            authz["<b>Authorization</b><br/>Policies по роли"]
            rl["<b>Rate Limiter</b><br/>короткие окна, in-memory"]
            idem["<b>Idempotency-Key</b><br/>чтение заголовка → команда"]
            etag["<b>If-Match</b><br/>чтение заголовка → expected Version"]
        end

        subgraph modules["Modules"]
            conv["<b>Conversations</b><br/>Обращения, сообщения,<br/>назначение, статусы"]
            org["<b>Support Organization</b><br/>Команды, сотрудники, роли"]
            iam["<b>Identity</b><br/>UserAccount, CustomerProfile"]
            aim["<b>AI Assistance</b><br/>Запрос и чтение suggestions,<br/>Accept / Reject"]
            rep["<b>Reporting</b><br/>Отчёты, workload (read-only)"]
            aud["<b>Audit</b><br/>Чтение журнала"]
        end

        subgraph bb["Building Blocks"]
            uow["<b>Unit of Work</b><br/>Одна транзакция на команду"]
            outbox["<b>Outbox Writer</b><br/>Integration events<br/>в той же транзакции"]
        end
    end

    web -- "HTTPS/JSON" --> authn
    authn -- "JWKS" --> idp
    authn --> authz --> rl --> idem --> etag
    etag --> conv & org & iam & aim & rep & aud

    conv -- "IsActiveMember(agent, team)<br/>[in-process contract]" --> org
    conv -- "Current user → CustomerId / StaffId" --> iam
    aim -- "GetMessages(convId, upToSeq)<br/>[in-process contract]" --> conv

    conv & org & aim --> uow
    conv & org & aim --> outbox
    uow -- "SQL" --> db
    outbox -- "INSERT outbox" --> db
    rep -- "SELECT reporting views" --> db
    aud -- "SELECT audit schema" --> db
    iam -- "SQL" --> db

    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef component fill:#85bbf0,stroke:#5d82a8,color:#000
    classDef external fill:#999,stroke:#6b6b6b,color:#fff
    class web,db container
    class authn,authz,rl,idem,etag,conv,org,iam,aim,rep,aud,outbox,uow component
    class idp external
```

| Компонент | Ответственность | ADR |
|---|---|---|
| Authentication | Проверка JWT и сопоставление `sub` с `UserAccount` | [0011](../desicions/0011-external-identity-provider.md) |
| Authorization | Policies по роли. Принадлежность (Customer видит только свои обращения, Supervisor — обращения своих команд) проверяет Application модуля: для этого нужны данные обращения | [0013](../desicions/0013-module-internal-structure.md) |
| Rate Limiter | Короткие окна (1 msg/s, API requests) в памяти instance. Суточный лимит обращений и лимит AI requests проверяются в модулях | [0012](../desicions/0012-rate-limiting.md) |
| Idempotency-Key | Только читает заголовок (нет или не UUID → 400) и передаёт ключ в команду. Дубли отсекает таблица ключей в схеме модуля | [0014](../desicions/0014-idempotency-keys-table.md) |
| If-Match | Только читает заголовок (нет → 428) и передаёт ожидаемую версию в команду. Проверка — при сохранении агрегата (несовпадение → 412) | [0007](../desicions/0007-optimistic-concurrency.md) |
| Модули | Бизнес-логика bounded contexts из domain-model. Внутреннее устройство — [architecture.md](architecture.md) | [0002](../desicions/0002-module-boundaries.md), [0013](../desicions/0013-module-internal-structure.md) |
| Unit of Work + Outbox Writer | Изменение агрегата и integration event коммитятся атомарно | [0003](../desicions/0003-transactional-outbox-postgresql-queue.md) |

У модуля Notifications нет пользовательских операций. Он полностью асинхронный и работает только в
Worker.

### 1.1 Внутреннее устройство модуля (на примере Conversations)

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
flowchart LR
    subgraph convm["Conversations Module"]
        direction LR
        ep["<b>Endpoints</b><br/>[inbound adapter, Api]<br/>Minimal API route group<br/>/conversations/*"]
        eh["<b>EventHandlers / Jobs</b><br/>[inbound adapter, Worker]<br/>авто-закрытие Resolved"]
        app["<b>Application</b><br/>Command / Query handlers,<br/>порты"]
        dom["<b>Domain</b><br/>Conversation, Message, Category<br/>инварианты, domain events"]
        infra["<b>Infrastructure</b><br/>[outbound adapter]<br/>Repositories, EF Core mappings,<br/>schema conversations"]
        contract["<b>Contracts (public)</b><br/>Integration events,<br/>query interfaces"]
    end
    other["Другие модули"]
    orgc["SupportOrganization.Contracts"]

    ep --> app
    eh --> app
    app --> dom
    infra -. "реализует порты" .-> app
    infra --> dom
    app -- "IsActiveMember" --> orgc
    app -. "публикует" .-> contract
    other -- "зависят только от" --> contract

    classDef component fill:#85bbf0,stroke:#5d82a8,color:#000
    class ep,eh,app,dom,infra,contract component
```

Слои — папки внутри проекта `SupportFlow.Modules.Conversations`, `Contracts` — отдельный проект. Правила
зависимостей — [architecture.md §3–§4](architecture.md).

### 1.2 Правила границ модулей ([ADR-0002](../desicions/0002-module-boundaries.md))

1. Модуль зависит **только от `Contracts`** другого модуля.
2. Модуль читает и пишет **только свою схему БД**, без cross-schema FK и JOIN. Исключение — `reporting`
   views ([ADR-0010](../desicions/0010-reporting-read-only-views.md)).
3. Синхронные межмодульные вызовы допускаются **только для чтения**. Изменения в другом модуле происходят
   только через integration events.
4. Правила проверяются архитектурными тестами.

### 1.3 Соглашения API

**Пагинация (§10, §13).** Ни один список не возвращается без ограничения.

| Ресурс | Пагинация | Размер страницы [Assumption] |
|---|---|---|
| Список обращений (Customer, Agent, Supervisor) | Keyset по `(last_activity_at DESC, id)`, непрозрачный `cursor` | по умолчанию 20, максимум 100 |
| История сообщений | Keyset по `Seq`: `?afterSeq=N` (новые) / `?beforeSeq=N` (старые) | по умолчанию 50, максимум 200 |
| Suggestions обращения | Последние по каждому `Kind` | — |
| Отчёты | Обязательный период и фильтр по командам Supervisor (ADR-0010) | — |

**Ограничения размера.** Тело запроса отправки сообщения и создания обращения (содержит первое сообщение) — не
больше 16 KB (сообщение ≤ 10 KB плюс метаданные) **[Assumption]**, остальные команды — не больше 4 KB
**[Assumption]**. Превышение → `413`.

**Коды ошибок.**

| Код | Когда |
|---|---|
| `400` | Неверный формат запроса, в том числе отсутствующий или не-UUID `Idempotency-Key` |
| `403` | Нет прав на ресурс (чужое обращение, обращение вне команд Supervisor) |
| `409` | Нарушение бизнес-правила или конфликт без `If-Match`: claim уже взят, недопустимый переход статуса, обращение `Closed`, лимит 1000 сообщений |
| `412` | `If-Match` не совпадает с текущей `Version` ([ADR-0007](../desicions/0007-optimistic-concurrency.md)) |
| `413` | Превышен размер запроса |
| `422` | `Idempotency-Key` уже использован с другим телом запроса ([ADR-0014](../desicions/0014-idempotency-keys-table.md)) |
| `428` | Команда изменения без `If-Match` |
| `429` | Превышен rate limit, с `Retry-After` ([ADR-0012](../desicions/0012-rate-limiting.md)) |

---

## 2. SupportFlow Worker

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
flowchart TB
    db[("<b>SupportFlow DB</b><br/>[PostgreSQL]")]
    ai["<b>AI Provider</b>"]
    np["<b>Notification Provider</b>"]

    subgraph worker["SupportFlow Worker [Container]"]
        direction TB
        disp["<b>Outbox Dispatcher</b><br/>Polling outbox, SKIP LOCKED,<br/>at-least-once delivery"]

        subgraph handlers["Integration Event Handlers (inbox dedup)"]
            h_ai["<b>AI Request Handler</b><br/>ConversationOpened →<br/>AISuggestion Requested"]
            h_dec["<b>AI Decision Handler</b><br/>CategoryChanged / PriorityChanged<br/>with suggestion → Accepted"]
            h_not["<b>Notification Handler</b><br/>Agent message, Resolved, Closed →<br/>Notification Pending"]
            h_aud["<b>Audit Handler</b><br/>значимые события → AuditEvent"]
        end

        aiw["<b>AI Job Processor</b><br/>Requested → Processing (lease),<br/>concurrency limit, timeout, retry"]
        acl_ai["<b>AI Provider Adapter</b><br/>[ACL] prompt building,<br/>response parsing"]
        ns["<b>Notification Sender</b><br/>Pending → Processing (lease),<br/>retry по NextAttemptAt"]
        acl_np["<b>Notification Provider Adapter</b><br/>[ACL], idempotency key = NotificationId"]
        sched["<b>Scheduler</b><br/>Авто-закрытие Resolved,<br/>retention, очистка outbox / inbox"]

        subgraph contracts["Contracts других модулей (in-process, только чтение)"]
            conv_c["<b>Conversations.Contracts</b><br/>GetMessages(convId, upToSeq)"]
            iam_c["<b>Identity.Contracts</b><br/>GetContactInfo(customerId)"]
        end
    end

    disp -- "SELECT … FOR UPDATE SKIP LOCKED" --> db
    disp --> h_ai & h_dec & h_not & h_aud
    h_ai & h_dec & h_not & h_aud -- "INSERT / UPDATE + inbox" --> db
    h_not -- "снимок адреса получателя" --> iam_c

    aiw -- "claim job (lease), запись результата<br/>[schema ai]" --> db
    aiw -- "сообщения до InputUpToSeq" --> conv_c
    aiw --> acl_ai -- "HTTPS, вне транзакции" --> ai

    ns -- "claim notification (lease)<br/>[schema notifications]" --> db
    ns --> acl_np -- "HTTPS, вне транзакции" --> np

    conv_c & iam_c -- "SQL своих схем" --> db
    sched -- "команды модулей" --> db

    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef component fill:#85bbf0,stroke:#5d82a8,color:#000
    classDef external fill:#999,stroke:#6b6b6b,color:#fff
    class db container
    class disp,h_ai,h_dec,h_not,h_aud,aiw,acl_ai,ns,acl_np,sched,conv_c,iam_c component
    class ai,np external
```

Worker содержит те же модули, что и API. На диаграмме показаны только его inbound adapters (dispatcher,
handlers, job processors, scheduler) и контракты, через которые они читают данные других модулей.
Обработчики и jobs вызывают Application своего модуля ([architecture.md §13](architecture.md)).

**Обоснование:**

- **Обработка события и исполнение медленной работы разделены.** `AI Request Handler` только создаёт
  `AISuggestion(Requested)`: это быстро и выполняется в транзакции. Провайдера вызывает `AI Job Processor`.
  Поэтому медленный или недоступный AI не задерживает доставку остальных событий, а таблица
  `ai.suggestions` одновременно служит очередью AI-работ. Уведомления устроены так же.
- **Job берётся по lease, внешний вызов — вне транзакции** ([ADR-0003](../desicions/0003-transactional-outbox-postgresql-queue.md)).
  Короткая транзакция переводит job в `Processing` с `LeaseUntil` и увеличивает `Attempts`. Вызов AI или
  Notification Provider идёт без открытой транзакции и соединения с БД. Результат пишется условным UPDATE
  `WHERE status = 'Processing' AND attempts = @myAttempt`. Job с истёкшим lease снова доступен другим
  instances. Notification Provider получает `NotificationId` как idempotency key: повторная отправка после
  сбоя не дублирует уведомление.
- **Retention** ([§4.8](../requiremenets.md)). Scheduler удаляет `AuditEvent` старше ~1 года, доставленные
  записи outbox/inbox и ключи идемпотентности старше 48 часов
  ([ADR-0014](../desicions/0014-idempotency-keys-table.md)). Архивация обращений старше ~3 лет на Stage 1 не реализована (см.
  [containers.md](containers.md), известные ограничения).
- **Inbox** (обработанные `EventId` на каждый обработчик) превращает at-least-once доставку в
  effectively-once обработку (§4.4).
- **Ограничение параллельных вызовов AI Provider** на instance (bulkhead): §6, «ограничение concurrency
  downstream-зависимостей».
- **Retry с exponential backoff, jitter и лимитом попыток**, после которого `Failed`/`Abandoned`. Так retry
  не увеличивает нагрузку неконтролируемо (§4.4).
- **Периодические задачи** берут работу через `SKIP LOCKED` или advisory lock, поэтому несколько instances
  Worker не выполняют одну задачу дважды.
- **Adapters (ACL)** изолируют модель внешних API от домена
  ([ADR-0009](../desicions/0009-ai-assistance-separate-context.md)).

---

## 3. Dynamic diagrams

### 3.1 Customer отправляет сообщение

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
sequenceDiagram
    autonumber
    actor C as Customer
    participant API as SupportFlow API
    participant DB as PostgreSQL
    participant W as Worker

    C->>API: POST /conversations/{id}/messages<br/>Idempotency-Key: K
    API->>API: JWT, rate limit 1 msg/s, size ≤ 16 KB, K — UUID
    API->>DB: SELECT idempotency_keys WHERE (caller_id, K, PostMessage)
    alt ключ найден, request hash совпал (повтор)
        API-->>C: 200 OK {существующее сообщение}
    else ключ найден, request hash отличается
        API-->>C: 422 Unprocessable Content
    else новый запрос
        API->>DB: BEGIN
        API->>DB: INSERT idempotency_keys (caller_id, K, PostMessage, hash, messageId)<br/>первой командой транзакции
        API->>DB: SELECT conversation … FOR UPDATE
        API->>API: ownership (CustomerId), Conversation.RegisterMessage(author):<br/>C4, C5, C8 → Seq = LastMessageSeq + 1,<br/>WaitingOnCustomer / Resolved → InProgress
        API->>DB: UPDATE conversation (last_message_seq, message_count,<br/>status, version + 1)
        API->>DB: INSERT message (id = messageId, seq, author_id)
        API->>DB: INSERT status_history (если статус изменился)
        API->>DB: INSERT outbox (MessagePosted [, ConversationStatusChanged])
        API->>DB: COMMIT
        API-->>C: 201 Created {seq}
    end
    Note over API,DB: Конкурентный повтор с тем же K ждёт на INSERT ключа, затем нарушение PK →<br/>ROLLBACK → поиск ключа → 200 (тот же hash) или 422 (другой hash)
    W->>DB: poll outbox (SKIP LOCKED)
    W->>W: Audit Handler, Notification Handler (если автор Agent)
```

См. [ADR-0004](../desicions/0004-conversation-and-message-aggregates.md),
[ADR-0005](../desicions/0005-message-ordering-seq.md), [ADR-0014](../desicions/0014-idempotency-keys-table.md).

### 3.2 Два агента одновременно берут обращение

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
sequenceDiagram
    autonumber
    actor A1 as Agent 1
    actor A2 as Agent 2
    participant API as SupportFlow API
    participant DB as PostgreSQL

    par
        A1->>API: POST /conversations/{id}/claim
    and
        A2->>API: POST /conversations/{id}/claim
    end
    API->>API: SupportOrganization.Contracts:<br/>A1, A2 активны и состоят в команде обращения (TeamId)
    API->>DB: SELECT conversation (оба запроса видят version = 5, status = New)
    API->>API: Conversation.Claim(A1) и Conversation.Claim(A2):<br/>инварианты C1–C2 выполнены
    API->>DB: BEGIN, UPDATE … SET assignee = A1, status = InProgress,<br/>version = 6 WHERE id = @id AND version = 5
    DB-->>API: 1 row
    API->>DB: INSERT assignment_history (Claim), status_history (New → InProgress),<br/>outbox (ConversationClaimed), COMMIT
    API->>DB: BEGIN, UPDATE … SET assignee = A2 … WHERE id = @id AND version = 5
    DB-->>API: 0 rows → ROLLBACK
    API-->>A1: 200 OK, ETag: v6
    API-->>A2: 409 Conflict
```

См. [ADR-0006](../desicions/0006-claim-via-conditional-update.md).

### 3.3 AI-классификация и принятие рекомендации

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
sequenceDiagram
    autonumber
    actor Ag as Agent
    participant API as SupportFlow API
    participant DB as PostgreSQL
    participant W as Worker
    participant AI as AI Provider

    Note over DB: ConversationOpened уже в outbox
    W->>DB: dispatch ConversationOpened
    W->>DB: INSERT ai.suggestions (Classification, Requested,<br/>InputUpToSeq = 1) ON CONFLICT DO NOTHING
    W->>DB: AI Job Processor: claim Requested (SKIP LOCKED) →<br/>Processing, LeaseUntil, Attempts + 1, COMMIT
    W->>DB: Conversations.Contracts: GetMessages(convId, upToSeq = 1)
    W->>AI: classify(messages ≤ seq 1) [timeout, вне транзакции]
    alt успех
        AI-->>W: category + confidence
        W->>DB: → Completed WHERE status = Processing AND attempts = @my
    else ошибка, попытки остались
        W->>DB: → Requested (retry с backoff)
    else отказ после N попыток
        W->>DB: suggestion → Failed
        Note over W,DB: Обращение обрабатывается без AI (§4.3).<br/>Agent может запросить Retry (ADR-0009)
    end
    Ag->>API: GET /conversations/{id}/suggestions
    API-->>Ag: suggestion (помечено как AI, FR-020)
    Ag->>API: PUT /conversations/{id}/category<br/>{categoryId, basedOnSuggestionId}, If-Match: v7
    API->>API: Conversation.ChangeCategory(categoryId, suggestionId)
    API->>DB: UPDATE conversation WHERE version = 7<br/>+ outbox CategoryChanged
    alt версия совпала
        API-->>Ag: 200 OK, ETag: v8
    else обращение изменилось
        API-->>Ag: 412 Precondition Failed
    end
    W->>DB: AI Decision Handler → Decision = Accepted
```

См. [ADR-0009](../desicions/0009-ai-assistance-separate-context.md),
[ADR-0007](../desicions/0007-optimistic-concurrency.md).

---

## 4. Отображение на код [Assumption]

```
src/
  SupportFlow.Api/                      # Host: HTTP pipeline, регистрация модулей
  SupportFlow.Worker/                   # Host: dispatcher, job processors, scheduler
  BuildingBlocks/
    SupportFlow.BuildingBlocks/         # Outbox, inbox, UoW, базовые типы domain events
  Modules/
    Conversations/
      SupportFlow.Modules.Conversations/  # папки Domain / Application / Infrastructure /
                                          #       Endpoints / EventHandlers
      SupportFlow.Modules.Conversations.Contracts/
    SupportOrganization/ …
    Identity/ …
    AIAssistance/ …
    Notifications/ …
    Audit/ …
    Reporting/ …
tests/
  SupportFlow.ArchitectureTests/        # Границы модулей и направление зависимостей слоёв
  SupportFlow.<Module>.Tests/
```

Один проект на модуль и отдельный `.Contracts` — минимальная структура, при которой правило «зависеть
только от Contracts» проверяет компилятор. Слои внутри модуля — папки, их зависимости проверяют
архитектурные тесты ([architecture.md §3](architecture.md),
[ADR-0013](../desicions/0013-module-internal-structure.md)).
