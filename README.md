# SupportFlow

Платформа обработки обращений в службу поддержки с AI-рекомендациями для сотрудников.

Модульный монолит на .NET 10: два процесса (API и Worker), одна PostgreSQL.

## Запуск

Требуется .NET SDK 10.

```bash
dotnet build SupportFlow.slnx
dotnet test --solution SupportFlow.slnx

dotnet run --project src/SupportFlow.Api      # http://localhost:5219
dotnet run --project src/SupportFlow.Worker   # http://localhost:5220 (только health)
```

Для разработки нужна локальная PostgreSQL (строка подключения в `appsettings.Development.json`). Интеграционные
тесты поднимают PostgreSQL сами через Testcontainers и требуют запущенный Docker.

Телеметрия локально: запустить `docker run -d -p 3000:3000 -p 4317:4317 -p 4318:4318 grafana/otel-lgtm`, задать
`OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` для API и Worker и открыть Grafana на http://localhost:3000.
Health checks: `/health/live`, `/health/ready`.

## Структура

```
src/
  SupportFlow.Api/       # HTTP API
  SupportFlow.Worker/    # фоновая обработка: outbox, AI, уведомления
  BuildingBlocks/        # общая инфраструктура
  Modules/<Module>/      # модуль и его публичный Contracts
tests/                   # архитектурные тесты
docs/                    # требования, domain model, архитектура, ADR
```

## Документация

- [Требования](docs/requiremenets.md)
- [Domain model](docs/domain-model.md)
- [Архитектура](docs/architecture/architecture.md)
- [ADR](docs/desicions/README.md)
