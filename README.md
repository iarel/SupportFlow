# SupportFlow

Платформа обработки обращений в службу поддержки с AI-рекомендациями для сотрудников.

Модульный монолит на .NET 10: два процесса (API и Worker), одна PostgreSQL.

## Запуск

Требуется .NET SDK 10.

```bash
dotnet build SupportFlow.slnx
dotnet test --solution SupportFlow.slnx

dotnet run --project src/SupportFlow.Api      # http://localhost:5219
dotnet run --project src/SupportFlow.Worker
```

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
