# ADR-0006: Текущий Assignee в Conversation, claim через условный UPDATE по `Version`

- **Status:** Proposed
- **Date:** 2026-09-30
- **Stage:** 1
- **Related:** requirements FR-006, FR-012, FR-013, §4.5; domain-model §3.2; ADR-0007, ADR-0013

## Problem

Два сотрудника не должны одновременно успешно взять одно обращение (§4.5). Нужно выбрать, где хранится
текущее назначение и как обеспечивается его единственность.

## Constraints

- Операция интерактивная: p95 ≤ 200 ms.
- История назначений нужна для отчётов (нагрузка, статистика).
- Несколько instances API: in-memory блокировки не работают.
- Инварианты агрегата проверяются в Domain, а не SQL-условиями (ADR-0013).

## Options

1. Назначения хранятся только в таблице `Assignment`, активное назначение уникально по partial unique
   index.
2. Пессимистическая блокировка (`SELECT … FOR UPDATE`) с последующей проверкой и записью.
3. `AssigneeId` хранится в `Conversation`. Claim — условный UPDATE с бизнес-условием
   `WHERE status = 'New' AND assignee_id IS NULL`, без загрузки агрегата.
4. `AssigneeId` хранится в `Conversation`. Агрегат загружается, `Conversation.Claim(agentId)` проверяет
   инварианты, сохранение — условный UPDATE по версии: `WHERE id = @id AND version = @loadedVersion`.
   `AssignmentHistory` — append-only журнал.

## Decision

Вариант 4.
- Claim: загрузка `Conversation` → `Claim(agentId)` проверяет C1–C2 (статус `New`, нет Assignee) →
  `UPDATE … SET assignee_id, status = 'InProgress', version = version + 1 WHERE id = @id AND version = @loaded`.
  Если затронуто 0 строк, кто-то изменил обращение раньше: ответ `409 Conflict`.
- В той же транзакции: `AssignmentHistory (Kind = Claim)`, `StatusHistory (New → InProgress)`, integration
  event `ConversationClaimed` в outbox.
- До загрузки агрегата Application проверяет через `SupportOrganization.Contracts`, что Agent активен и
  состоит в команде обращения (`TeamId`).
- Assign/Reassign от Supervisor: тот же механизм, но ожидаемая версия берётся из `If-Match` (ADR-0007).

## Trade-offs

**Плюсы:**
- Никаких удерживаемых блокировок. Гонку разрешает атомарность UPDATE в PostgreSQL.
- Инвариант «не больше одного Assignee» следует из схемы, а правила claim — из агрегата, в одном месте
  с остальными переходами статусов.
- Механизм совпадает с optimistic concurrency остальных команд (ADR-0007).

**Минусы:**
- Два round-trip (SELECT + UPDATE) вместо одного. В пределах p95 ≤ 200 ms это допустимо.
- Любое изменение обращения между загрузкой и UPDATE (например, новое сообщение) тоже даёт 409, хотя claim
  был бы корректен. Клиент повторяет claim; такие гонки редки.
- Текущее назначение и история хранятся в двух местах. История пишется строго в той же транзакции.
- Проверка членства в команде выполняется до UPDATE. Редкая гонка с изменением состава команды допускается.
- Вариант 3 отвергнут: правило claim дублировалось бы в SQL и в агрегате (ADR-0013).
