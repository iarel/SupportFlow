# ADR-0011: Внешний Identity Provider, JWT, stateless API

- **Status:** Proposed
- **Date:** 2026-09-30
- **Stage:** 1
- **Related:** requirements §4.3, §11, §14 (authentication, authorization); domain-model §5

## Problem

В MVP нужны authentication и authorization для трёх ролей: Customer, Support Agent, Supervisor. API должен
оставаться stateless (§4.3, §11).

## Constraints

- Критическое состояние не хранится в памяти instance (§4.3).
- Аутентификация не является ценностью продукта (generic subdomain).

## Options

1. Собственная аутентификация (ASP.NET Core Identity), cookie sessions.
2. Собственная аутентификация, JWT.
3. Внешний OIDC Identity Provider. API проверяет JWT (JWKS), роли в SupportFlow хранятся в модуле Identity.

## Decision

Вариант 3.
- Web Client проходит OIDC-логин у IdP и вызывает API с Bearer JWT.
- API проверяет подпись и срок токена и сопоставляет `sub` с `UserAccount`.
- Роли и принадлежность (Customer владеет обращением, Agent состоит в команде) проверяются в SupportFlow:
  это данные домена, а не IdP.
- Конкретный IdP (self-hosted или managed) не выбран **[Open question]**.

## Trade-offs

**Плюсы:**
- API полностью stateless, любой instance обрабатывает любой запрос.
- Пароли, MFA и восстановление доступа делегированы специализированной системе.

**Минусы:**
- Внешняя зависимость для логина. Уже выданные токены продолжают работать при недоступности IdP до
  истечения срока.
- Отзыв доступа вступает в силу только после истечения access token. Поэтому срок токена должен быть
  коротким.
- Нужна синхронизация пользователей IdP и `UserAccount` (создание при первом входе).
