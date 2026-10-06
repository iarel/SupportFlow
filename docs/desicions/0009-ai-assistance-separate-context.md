# ADR-0009: AI Assistance — отдельный контекст, AI не изменяет домен

- **Status:** Proposed
- **Date:** 2026-09-30
- **Stage:** 1
- **Related:** requirements §2.4, FR-016…FR-020, §4.1, §4.3, §13; domain-model §6; ADR-0003

## Problem

AI должен классифицировать обращение, предлагать приоритет, формировать summary и черновик ответа.
При этом AI не принимает окончательных решений, а его рекомендации должны быть явно отличимы от решений
Support Agent (§2.4, FR-020). AI медленный (до 10 s) и ненадёжный (§13).

## Constraints

- Отказ AI не должен влиять на основную функциональность (§4.3).
- AI-результаты хранятся ~3 года (§4.8), нужна воспроизводимость (какая модель и какой prompt).
- Нельзя выполнять `HTTP request → AI → HTTP response` синхронно (§13).

## Options

1. AI пишет результат прямо в поля `Conversation` (Category, Priority) с флагом «set by AI».
2. Отдельный контекст AI Assistance с агрегатом `AISuggestion`. Результат — только предложение, которое
   человек принимает обычной командой Conversations.

## Decision

Вариант 2.
- `AISuggestion { Kind, Status, InputUpToSeq, Attempts, LeaseUntil, Result, ModelVersion, PromptVersion,
  Decision }`, `Status: Requested | Processing | Completed | Failed`.
- Classification и Priority запускаются по `ConversationOpened`. Summary и ReplyDraft запускаются по запросу
  Agent **[Assumption]**.
- Исполнение асинхронное: event → job (`Requested`) → AI Job Processor → AI Provider через adapter (ACL),
  с timeout, retry и ограничением параллелизма.
- AI Job Processor берёт job по lease (`Requested → Processing`, `LeaseUntil`), вызывает AI Provider вне
  транзакции и записывает результат условным UPDATE (ADR-0003). Сообщения читает через
  `Conversations.Contracts` (`GetMessages(conversationId, upToSeq)`).
- После исчерпания попыток suggestion переходит в `Failed`. Agent может запросить повтор командой `Retry`:
  та же запись переводится `Failed → Requested`, `Attempts` сбрасывается. Ключ уникальности
  `(ConversationId, Kind, InputUpToSeq)` не меняется. `Retry` расходует лимит AI requests (ADR-0012).
- Если запрос Summary/ReplyDraft совпадает с существующей suggestion (тот же `InputUpToSeq`), возвращается
  она, а новый вызов AI не выполняется.
- Принятие: Agent вызывает `ChangeCategory(categoryId, basedOnSuggestionId)`. Событие переводит suggestion
  в `Accepted`. Отклонение — явная команда в AI Assistance.
- Ответ, основанный на черновике, отправляет Agent. `Message.BasedOnSuggestionId` сохраняет связь для
  трассируемости.

## Trade-offs

**Плюсы:**
- FR-020 выполняется структурно: состояние обращения меняет только человек.
- Отказ AI оставляет suggestion в `Failed` и никак не влияет на обращение. Повтор возможен без новых
  сообщений в обращении.
- Можно измерять качество AI (доля Accepted по версиям модели и prompt).

**Минусы:**
- Принятие рекомендации требует явного действия агента. Автоматической классификации без человека нет,
  и это соответствует §15.
- Отметка `Accepted` проставляется eventually: сразу после принятия suggestion ещё может числиться
  `Pending`.
- Suggestion может устареть. Если `LastMessageSeq > InputUpToSeq`, UI должен показать это.
