# ADR-0010: Reporting через read-only views

- **Status:** Proposed
- **Date:** 2026-09-30
- **Stage:** 1
- **Related:** requirements FR-014, FR-015, §4.9, §12.2; domain-model §9; ADR-0002

## Problem

Supervisor нужны operational reports (обращения по статусам, категориям и приоритетам, нагрузка агентов,
время обработки) и workload. Данные для них принадлежат модулю Conversations, а правила ADR-0002 запрещают
другим модулям читать его таблицы.

## Constraints

- На MVP отчётность может использовать основную PostgreSQL (§4.9).
- p95 ≤ 2 s для простых отчётов и ≤ 5 s для сложных.
- Владение данными не должно размываться (§12.2).

## Options

1. Reporting читает внутренние таблицы Conversations напрямую.
2. Conversations публикует read-only SQL views в схеме `reporting` как часть своего контракта.
3. Reporting строит собственные projection-таблицы из integration events.

## Decision

Вариант 2.
- Views (`reporting.conversations_by_status`, `reporting.agent_workload`, `reporting.status_durations` и
  т. п.) создаются миграциями модуля Conversations и версионируются как контракт.
- Модуль Reporting читает только схему `reporting`.
- Workload вычисляется запросом по индексу `(assignee_id, status)`, а не хранится счётчиком.

## Trade-offs

**Плюсы:**
- Минимум кода, данные отчётов всегда актуальны.
- Внутренние таблицы Conversations можно менять, сохраняя views.

**Минусы:**
- Отчётные запросы выполняются на той же БД, что и транзакционная нагрузка, и конкурируют с ней.
- Views — SQL-контракт, который сложнее тестировать и версионировать, чем C#-контракт.
- Вариант 3 отложен: он требует обработчиков, backfill и мониторинга отставания projections, а на
  текущем этапе нет данных, что это нужно.
