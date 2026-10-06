# C4 · Level 1 — System Context

> **Архитектурный этап: Stage 1 — Naive modular monolith.**
> Документ описывает только текущее состояние архитектуры. Изменения фиксируются через ADR
> в `docs/desicions/`.
>
> Связанные документы: [containers.md](containers.md) · [components.md](components.md) ·
> [architecture.md](architecture.md) · [domain-model.md](../domain-model.md) ·
> [requiremenets.md](../requiremenets.md)

## Легенда

| Элемент | Обозначение |
|---|---|
| Person | тёмно-синий |
| Система SupportFlow | синий |
| Внешняя система | серый |
| Сплошная стрелка | синхронное взаимодействие |
| Пунктирная стрелка | асинхронное взаимодействие |

## Диаграмма

```mermaid
%%{init: {"theme":"base","themeVariables":{"primaryColor":"#dbe9f6","primaryTextColor":"#0b2540","primaryBorderColor":"#5d82a8","lineColor":"#e67700","secondaryColor":"#dbe9f6","tertiaryColor":"#f1f5fa","clusterBkg":"#f1f5fa","clusterBorder":"#5d82a8","titleColor":"#0b4884","edgeLabelBackground":"#fff4e6","textColor":"#e67700","actorBkg":"#438dd5","actorTextColor":"#ffffff","actorBorder":"#2e6295","actorLineColor":"#e67700","signalColor":"#e67700","signalTextColor":"#e67700","labelBoxBkgColor":"#dbe9f6","labelBoxBorderColor":"#5d82a8","labelTextColor":"#0b2540","loopTextColor":"#e67700","noteBkgColor":"#fff4e6","noteTextColor":"#0b2540","noteBorderColor":"#e67700","activationBkgColor":"#dbe9f6","attributeBackgroundColorOdd":"#ffffff","attributeBackgroundColorEven":"#f1f5fa"}}}%%
flowchart TB
    customer(["<b>Customer</b><br/>[Person]<br/>Создаёт обращения, переписывается с поддержкой"])
    agent(["<b>Support Agent</b><br/>[Person]<br/>Обрабатывает обращения, отвечает клиентам"])
    supervisor(["<b>Supervisor</b><br/>[Person]<br/>Распределяет нагрузку, смотрит отчёты"])

    sf["<b>SupportFlow</b><br/>[Software System]<br/>Обработка обращений, назначение,<br/>AI-рекомендации, отчёты"]

    idp["<b>Identity Provider</b><br/>[External System]<br/>Аутентификация, OIDC / JWT"]
    ai["<b>AI Provider</b><br/>[External System]<br/>LLM API: классификация, приоритет,<br/>summary, черновик ответа"]
    np["<b>Notification Provider</b><br/>[External System]<br/>Доставка уведомлений клиентам"]

    customer -- "Создаёт обращения, пишет сообщения<br/>[HTTPS/JSON]" --> sf
    agent -- "Берёт и обрабатывает обращения<br/>[HTTPS/JSON]" --> sf
    supervisor -- "Назначает, смотрит нагрузку и отчёты<br/>[HTTPS/JSON]" --> sf

    customer & agent & supervisor -- "Входят в систему<br/>[OIDC]" --> idp
    sf -- "Проверяет токены<br/>[JWKS]" --> idp
    sf -. "Запрашивает рекомендации<br/>[HTTPS]" .-> ai
    sf -. "Отправляет уведомления<br/>[HTTPS]" .-> np
    np -. "Уведомляет о событиях обращения" .-> customer

    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef system fill:#1168bd,stroke:#0b4884,color:#fff
    classDef external fill:#999,stroke:#6b6b6b,color:#fff
    class customer,agent,supervisor person
    class sf system
    class idp,ai,np external
```

## Элементы

| Элемент | Тип | Роль | Требования |
|---|---|---|---|
| Customer | Person | Создаёт обращения, пишет сообщения, смотрит историю своих обращений | FR-001…FR-004 |
| Support Agent | Person | Берёт обращения в работу, отвечает, меняет статус, категорию и приоритет | FR-005…FR-010 |
| Supervisor | Person | Назначает и переназначает обращения, смотрит нагрузку и отчёты | FR-011…FR-015 |
| SupportFlow | Software System | Разрабатываемая система | — |
| Identity Provider | External | Аутентификация пользователей, выдача JWT | MVP scope: authentication |
| AI Provider | External | Внешний LLM API | FR-016…FR-019 |
| Notification Provider | External | Доставка уведомлений клиентам | FR-021 |

## Обоснование

- **AI — внешняя система, а не Person.** В требованиях AI перечислен среди акторов. Но на уровне
  системного контекста это внешний сервис, который вызывает SupportFlow, а не пользователь, который
  инициирует действия. Итоговые решения принимает Support Agent (§2.4, FR-020).
- **Взаимодействие с AI и Notification Provider асинхронное** (пунктир). Оба некритичны: их недоступность
  не должна блокировать работу с обращениями (§4.3). Поэтому ни один пользовательский HTTP-запрос не ждёт
  ответа этих систем.
- **Identity Provider внешний** ([ADR-0011](../desicions/0011-external-identity-provider.md)).
  Аутентификация — generic subdomain. SupportFlow только проверяет JWT и сопоставляет пользователя со
  своими ролями.
- **Одна система, а не набор сервисов.** SupportFlow развёртывается как модульный монолит
  ([ADR-0001](../desicions/0001-modular-monolith-api-and-worker.md)). Внутреннее устройство показано в
  [containers.md](containers.md).
