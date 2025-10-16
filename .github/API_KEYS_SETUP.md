# Резюме: Добавление API ключей для SimpleCorsProxy через GitHub Secrets

## ? Что было сделано:

### 1. **Обновлен SimpleCorsProxyService.cs**
- Добавлена поддержка опциональной авторизации
- Токен можно передать через параметр `token` или заголовок `Authorization: Bearer <token>`
- Авторизация контролируется через настройку `SimpleCorsProxy:RequireAuth`

### 2. **Обновлен Program.cs**
- Эндпоинт `/proxy` теперь принимает опциональный параметр `token`
- Обновлена документация Swagger

### 3. **Обновлены конфигурационные файлы**

**appsettings.json:**
```json
{
  "SimpleCorsProxy": {
    "RequireAuth": false  // Development - без авторизации
  }
}
```

**appsettings.Production.json:**
```json
{
  "SimpleCorsProxy": {
    "RequireAuth": true  // Production - требует авторизацию
  }
}
```

### 4. **Обновлены GitHub Actions Workflows**

Оба workflow (`.github/workflows/deploy.yml` и `deploy-ghcr.yml`) теперь:
- Создают `appsettings.Production.json` на сервере из GitHub Secrets
- Используют секреты:
  - `API_KEYS` - JSON массив с API ключами
  - `SIMPLE_CORS_REQUIRE_AUTH` - Включить/выключить авторизацию для `/proxy`

### 5. **Обновлена документация**

- **README.md**: Добавлена информация об авторизации для обоих эндпоинтов
- **.github/DEPLOYMENT.md**: Добавлены новые секреты `API_KEYS` и `SIMPLE_CORS_REQUIRE_AUTH`
- **.github/QUICKSTART.md**: Обновлен чеклист с новыми секретами
- **.env.example**: Добавлены примеры новых переменных

## ?? Настройка GitHub Secrets:

### Обязательные секреты:

1. **API_KEYS**
   ```
   Формат: ["key1", "key2", "key3"]
   Пример: ["a1b2c3d4e5f6", "x9y8z7w6v5u4"]
   ```
   
   Генерация безопасных ключей:
   ```bash
   openssl rand -hex 32
   ```

### Опциональные секреты:

2. **SIMPLE_CORS_REQUIRE_AUTH**
   ```
   Значение: true или false
   По умолчанию: true
   ```

## ?? Примеры использования:

### Development (без авторизации):
```bash
curl "http://localhost:5000/proxy?url=https://api.example.com/data"
```

### Production (с авторизацией):

**Через параметр запроса:**
```bash
curl "https://your-domain.com/proxy?url=https://api.example.com/data&token=your-api-key"
```

**Через заголовок Authorization:**
```bash
curl "https://your-domain.com/proxy?url=https://api.example.com/data" \
  -H "Authorization: Bearer your-api-key"
```

## ?? Преимущества:

? **Безопасность**: API ключи хранятся в GitHub Secrets, не в репозитории  
? **Гибкость**: Можно включить/выключить авторизацию для `/proxy`  
? **Совместимость**: В development режиме работает без токенов  
? **Удобство**: Поддержка двух способов передачи токена  

## ?? Важно:

1. **НИКОГДА** не коммитьте файл `appsettings.Production.json` с реальными ключами в репозиторий
2. Используйте длинные случайные ключи (минимум 32 символа)
3. В production всегда включайте авторизацию (`SimpleCorsProxy:RequireAuth = true`)
4. Регулярно меняйте API ключи
5. Используйте HTTPS на production

## ?? Деплой:

После настройки секретов:
```bash
git add .
git commit -m "feat: add API key auth for SimpleCorsProxy"
git push origin main
```

GitHub Actions автоматически:
1. Создаст `appsettings.Production.json` с вашими ключами
2. Развернет обновленное приложение
3. Новые контейнеры будут требовать авторизацию (если `SIMPLE_CORS_REQUIRE_AUTH=true`)

## ? FAQ:

**Q: Можно ли использовать разные ключи для `/web` и `/proxy`?**  
A: Нет, используется один массив `ApiKeys` для обоих эндпоинтов.

**Q: Что если я хочу, чтобы `/proxy` работал без авторизации на production?**  
A: Установите секрет `SIMPLE_CORS_REQUIRE_AUTH=false` в GitHub.

**Q: Как добавить новый ключ без пересборки?**  
A: Обновите секрет `API_KEYS` в GitHub и запустите новый деплой.

**Q: Работают ли старые запросы без токена?**  
A: Если `SimpleCorsProxy:RequireAuth=false`, то да. Иначе будет 401 Unauthorized.

## ?? Дополнительные материалы:

- Полная документация: [.github/DEPLOYMENT.md](.github/DEPLOYMENT.md)
- Быстрый старт: [.github/QUICKSTART.md](.github/QUICKSTART.md)
- Инструкции по ручному обновлению: [MANUAL_UPDATE_INSTRUCTIONS.md](MANUAL_UPDATE_INSTRUCTIONS.md)
