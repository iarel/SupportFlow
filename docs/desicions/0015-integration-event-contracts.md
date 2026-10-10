# ADR-0015: Контракт integration events и запись в outbox

- **Status:** Proposed
- **Date:** 2026-10-09
- **Stage:** 1
- **Related:** architecture.md §14; domain-model §3.2, §7; ADR-0002, ADR-0003, ADR-0004, ADR-0008, ADR-0009

## Problem

Integration events объявляются в `Contracts` модуля (architecture.md §14), а `Contracts` зависят только от BCL
(ADR-0002). Поэтому у integration events не может быть общего базового типа из BuildingBlocks. При этом
потребителям нужен идентификатор события: inbox дедуплицирует по `(HandlerName, EventId)`, Notifications и
Audit — по `SourceEventId` (ADR-0008). Нужно решить, где живут метаданные события и как Application пишет
события в outbox.

## Constraints

- `Contracts` зависят только от BCL (ADR-0002).
- Integration event пишется в outbox в той же транзакции, что и изменение агрегата (ADR-0003).
- Доставка at-least-once, потребители идемпотентны по идентификатору события (ADR-0008).
- Без новой инфраструктуры (requirements §13).

## Options

1. **Базовый тип или marker interface в BuildingBlocks.** Нарушает ADR-0002.
2. **Отдельная BCL-only сборка с абстракциями событий.** От неё зависят `Contracts` всех модулей, это
   исключение из правила ADR-0002.
3. **`EventId` и `OccurredAt` в каждом record по соглашению.** Компилятор соглашение не проверяет,
   метаданные доставки смешиваются с данными события.
4. **Record содержит только бизнес-данные, `EventId` — метаданные строки outbox.**

## Decision

Вариант 4.

- Integration event — `sealed record` в `Contracts` модуля, только с бизнес-данными и `OccurredAt`.
- Порт `IIntegrationEventOutbox.Add<TEvent>(TEvent)` объявлен в BuildingBlocks.Application. Запись выполняется
  в текущем unit of work и коммитится или откатывается вместе с изменением агрегата.
- `EventId` присваивает outbox при вставке строки. Dispatcher передаёт его обработчику вместе с событием, inbox
  и `SourceEventId` используют именно его.
- Domain events агрегата переводятся в integration events явно, в Application модуля. Domain event без
  сопоставления — ошибка, а не молчаливый пропуск.
- События не содержат тела сообщения. Потребители читают сообщения через запросы `Contracts`.
- `MessagePosted` порождает `Conversation`, потому что именно он выдаёт `Seq` и проверяет инварианты
  сообщения. `Message` — неизменяемая запись и domain events не порождает.
- `Conversation.Open` публикует `ConversationOpened` и `MessagePosted` с `Seq = 1`: у каждого `Message`
  ровно один `MessagePosted`, и потребителям сообщений не нужен особый случай для первого сообщения.
  AI запускает классификацию по `ConversationOpened` (ADR-0009), Notifications реагируют только на
  `MessagePosted` от Agent (domain-model §7), поэтому лишних срабатываний нет.
- Outbox хранит стабильное имя типа события, а не имя CLR-типа. Механизм задания имени — **[Assumption]**,
  фиксируется при реализации Infrastructure.

## Trade-offs

**Плюсы:**
- `Contracts` остаются BCL-only, исключение из ADR-0002 не нужно.
- Метаданные доставки отделены от данных события.
- Новый domain event нельзя забыть опубликовать незаметно.

**Минусы:**
- Компилятор не ограничивает тип, переданный в `Add`. Нужен архитектурный тест.
- Переименование события требует сохранить его хранимое имя, иначе записи outbox в полёте не доставятся.
- Лишняя строка outbox на каждое открытие обращения (`MessagePosted` для первого сообщения).
- Вариант 1 отвергнут: нарушает ADR-0002. Вариант 2 вводит общую зависимость всех контрактов ради одного
  свойства. Вариант 3 полагается на соглашение и смешивает метаданные с данными.
