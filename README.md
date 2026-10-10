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

## Production-like запуск

Образы API и Worker, миграции отдельным шагом, Keycloak вместо Identity Provider, Grafana LGTM (ADR-0017):

```bash
docker compose -f deploy/docker-compose.yml up --build -d
```

API — http://localhost:8080, health Worker — http://localhost:8081, Keycloak — http://localhost:8180
(admin/admin), Grafana — http://localhost:3000. Сервис `migrate` применяет миграции и завершается; API и Worker
стартуют только после его успешного завершения.

Токен локального пользователя (`customer`/`customer` или `customer2`/`customer2`):

```bash
TOKEN=$(curl -s -d grant_type=password -d client_id=supportflow-dev -d username=customer -d password=customer \
  http://localhost:8180/realms/supportflow/protocol/openid-connect/token | sed -E 's/.*"access_token":"([^"]+)".*/\1/')

curl -i -X POST http://localhost:8080/conversations -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: $(uuidgen)" -H "Content-Type: application/json" \
  -d '{"subject":"Вопрос","firstMessage":"Здравствуйте"}'
```

Остановить и удалить данные: `docker compose -f deploy/docker-compose.yml down -v`.

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
