# CarDatabase API

ASP.NET Core Web API для управления базой данных автомобилей с аутентификацией JWT.

## Технологии

- ASP.NET Core 9.0
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- xUnit + FluentAssertions (тестирование)

## Функционал

- ✅ CRUD операции для машин и владельцев
- ✅ JWT аутентификация
- ✅ Связь многие-к-одному (Car → Owner)
- ✅ Unit и Integration тесты (20+ тестов)

## Запуск проекта
```bash
cd CarDatabase
dotnet restore
dotnet run
```

API доступен на: `https://localhost:5225`

## Запуск тестов
```bash
dotnet test
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Вход

### Cars
- `GET /api/car` - Получить все машины
- `GET /api/car/{id}` - Получить машину по ID
- `POST /api/car` - Создать машину
- `PUT /api/car/{id}` - Обновить машину
- `PATCH /api/car/{id}` - Частично обновить
- `DELETE /api/car/{id}` - Удалить машину

### Owners
- `GET /api/owner` - Получить всех владельцев
- `GET /api/owner/{id}` - Получить владельца
- `POST /api/owner` - Создать владельца
- `PUT /api/owner/{id}` - Обновить владельца
- `DELETE /api/owner/{id}` - Удалить владельца
