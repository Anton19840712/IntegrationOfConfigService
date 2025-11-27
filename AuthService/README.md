# AuthService

Сервис аутентификации и авторизации с поддержкой JWT, TOTP 2FA, RBAC и rate limiting.

## Требования

- .NET 8.0
- Docker (для Tarantool)
- PostgreSQL (для хранения пользователей)
- RabbitMQ (для событий)

## Быстрый старт

### 1. Запуск Tarantool (обязательно)

```bash
docker-compose -f docker-compose.tarantool.yml up -d
```

Tarantool используется для:
- Кеширования пользователей и ролей
- Rate limiting попыток логина
- Хранения refresh tokens

### 2. Запуск AuthService

```bash
dotnet run --project API
```

## Проверка

```bash
# Health check
curl http://localhost:5000/health

# Tarantool health
docker exec authservice-tarantool tarantoolctl connect admin:secret@localhost:3301 -e "health_check()"
```

## API Documentation & Sandbox

- **Swagger UI**: http://localhost:5000/swagger
- **Tarantool Cache Sandbox**: http://localhost:5000/tarantool-cache-sandbox.html
  _Файл: `API/wwwroot/tarantool-cache-sandbox.html`_
- **Health Check**: http://localhost:5000/health

## Конфигурация

### Tarantool

```json
{
  "Tarantool": {
    "Host": "localhost",
    "Port": 3301,
    "Username": "admin",
    "Password": "secret"
  }
}
```

### Cache Settings (в appsettings.json)

```json
{
  "Cache": {
    "UserCacheEnabled": true,
    "UserCacheTtlSeconds": 100,
    "LoginRateLimitingEnabled": true,
    "LoginRateLimitMaxAttempts": 5,
    "LoginRateLimitWindowSeconds": 300,
    "LoginRateLimitBlockDurationSeconds": 900
  }
}
```

**Параметры:**
- `UserCacheEnabled` - включить кеширование пользователей
- `UserCacheTtlSeconds` - TTL кеша пользователей (секунды)
- `LoginRateLimitingEnabled` - включить rate limiting
- `LoginRateLimitMaxAttempts` - максимум попыток логина
- `LoginRateLimitWindowSeconds` - временное окно для подсчета попыток
- `LoginRateLimitBlockDurationSeconds` - время блокировки после превышения лимита

## Архитектура

- **Clean Architecture**: API → Application → Infrastructure → Domain
- **Tarantool**: In-memory кеш и rate limiting
- **PostgreSQL**: Персистентное хранилище
- **RabbitMQ**: Асинхронные события (UserLoggedIn, UserCreated, etc.)

## Fail-Safe

Сервис работает в режиме **fail-open**: если Tarantool недоступен, аутентификация продолжает работать (без кеша и rate limiting).
