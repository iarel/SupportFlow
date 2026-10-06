# ADR-0010: Reporting через read-only views

- **Status:** Proposed
- **Date:** 2026-09-30
- **Stage:** 1
- **Related:** requirements FR-014, FR-015, §4.9, §12.2; domain-model §9; ADR-0002

## Problem

Supervisor нужны operational reports (обращения по статусам, категориям и приоритетам, нагрузка агентов,
время обработки) и workload. Данные для них принадлежат модулям Conversations (обращения, история) и
Support Organization (сотрудники, команды, зона ответственности Supervisor), а правила ADR-0002 запрещают
другим модулям читать их таблицы.

## Constraints

- На MVP отчётность может использовать основную PostgreSQL (§4.9).
- p95 ≤ 2 s для простых отчётов и ≤ 5 s для сложных.
- Владение данными не должно размываться (§12.2).

## Options

1. Reporting читает внутренние таблицы модулей напрямую.
2. Модули-владельцы публикуют read-only SQL views в схеме `reporting` как часть своего контракта.
3. Reporting строит собственные projection-таблицы из integration events.

## Decision

Вариант 2.
- Conversations публикует `reporting.conversations_by_status`, `reporting.agent_workload`,
  `reporting.status_durations` и т. п.
- Support Organization публикует `reporting.staff_members` (сотрудник, команда, активность) и
  `reporting.team_supervisors` (зона ответственности Supervisor).
- Каждая view создаётся миграциями модуля-владельца и версионируется как его контракт.
- Модуль Reporting читает только схему `reporting` и может соединять (JOIN) views разных модулей. Это
  единственное разрешённое исключение из запрета cross-schema JOIN (ADR-0002).
- Каждый отчёт фильтруется по командам текущего Supervisor и по периоду. Запросы без ограничения периода
  не допускаются (§10).
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
