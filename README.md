# HTTP Proxy Server with Configurable Pipeline

[![Deploy to Production](https://github.com/AzerQ/WebCorsProxy/actions/workflows/deploy.yml/badge.svg)](https://github.com/AzerQ/WebCorsProxy/actions/workflows/deploy.yml)

Этот проект представляет собой HTTP прокси-сервер с гибким конфигурируемым пайплайном предобработки запросов и постобработки ответов.

## Архитектура

### Основные компоненты

1. **Интерфейсы пайплайнов**:
   - `IRequestProcessor` - интерфейс для обработчиков запросов
   - `IResponseProcessor` - интерфейс для обработчиков ответов
   - `IRequestPipeline` - интерфейс пайплайна обработки запросов
   - `IResponsePipeline` - интерфейс пайплайна обработки ответов

2. **Обработчики запросов**:
   - `ValidationProcessor` - валидация URL и параметров запроса
   - `HeadersProcessor` - обработка заголовков запроса

3. **Обработчики ответов**:
   - `ContentDetectionProcessor` - определение типа контента
   - `HeadersResponseProcessor` - обработка заголовков ответа
   - `HtmlContentProcessor` - обработка HTML контента
   - `CssContentProcessor` - обработка CSS контента
   - `JsContentProcessor` - обработка JavaScript контента

4. **Реализации пайплайнов**:
   - `DefaultRequestPipeline` - пайплайн обработки запросов по умолчанию
   - `DefaultResponsePipeline` - пайплайн обработки ответов по умолчанию

## Конфигурация

Конфигурация пайплайнов задается в файле `appsettings.json` в секции `ProxyPipeline`:

```json
{
  "ProxyPipeline": {
    "RequestPipeline": {
      "EnabledProcessors": [
        "ValidationProcessor",
        "HeadersProcessor"
      ],
      "ProcessorSettings": {
        "ValidationProcessor": {
          "Order": 0,
          "Enabled": true
        },
        "HeadersProcessor": {
          "Order": 2,
          "Enabled": true
        }
      }
    },
    "ResponsePipeline": {
      "EnabledProcessors": [
        "ContentDetectionProcessor",
        "HeadersResponseProcessor",
        "HtmlContentProcessor",
        "CssContentProcessor",
        "JsContentProcessor"
      ],
      "ProcessorSettings": {
        "ContentDetectionProcessor": {
          "Order": 0,
          "Enabled": true
        },
        "HeadersResponseProcessor": {
          "Order": 1,
          "Enabled": true
        },
        "HtmlContentProcessor": {
          "Order": 2,
          "Enabled": true
        },
        "CssContentProcessor": {
          "Order": 2,
          "Enabled": true
        },
        "JsContentProcessor": {
          "Order": 2,
          "Enabled": true
        }
      }
    }
  }
}
```

### Параметры конфигурации

- `EnabledProcessors` - список включенных обработчиков
- `ProcessorSettings` - настройки для отдельных обработчиков:
  - `Order` - порядок выполнения обработчика
  - `Enabled` - включен ли обработчик
  - `Parameters` - дополнительные параметры обработчика

## Использование

### Запуск сервера

```bash
dotnet run
```

### Эндпоинты

- `/web?url={url}&token={token}` - основной эндпоинт с новым пайплайном обработки контента (HTML, CSS, JS)
- `/proxy?url={url}&token={token}` - простой CORS прокси без обработки контента, возвращает контент as-is

**⚠️ Авторизация:**
- В **development** режиме `/proxy` не требует токен авторизации
- В **production** режиме оба эндпоинта требуют токен авторизации (настраивается через `SimpleCorsProxy:RequireAuth`)

### Примеры запросов

**Основной эндпоинт с обработкой контента:**
```
GET /web?url=https://example.com&token=your-api-key-1
```

**Простой CORS прокси без обработки:**
```
# Development (токен опционален)
GET /proxy?url=https://api.example.com/data

# Production (токен обязателен)
GET /proxy?url=https://api.example.com/data&token=your-api-key-1

# Или через заголовок Authorization
GET /proxy?url=https://api.example.com/data
Header: Authorization: Bearer your-api-key-1
```

Эндпоинт `/proxy` идеально подходит для:
- Проксирования API запросов
- Получения изображений и других медиа-файлов
- Обхода CORS ограничений без изменения контента
- Быстрого доступа к ресурсам

## Расширение функциональности

### Добавление нового обработчика запросов

1. Создать класс, реализующий `IRequestProcessor`
2. Зарегистрировать его в `Program.cs`
3. Добавить в конфигурацию при необходимости

```csharp
public class CustomRequestProcessor : IRequestProcessor
{
    public int Order => 3;
    
    public async Task ProcessAsync(RequestProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Ваша логика обработки
        await Task.CompletedTask;
    }
}
```

### Добавление нового обработчика ответов

1. Создать класс, реализующий `IResponseProcessor`
2. Зарегистрировать его в `Program.cs`
3. Добавить в конфигурацию при необходимости

```csharp
public class CustomResponseProcessor : IResponseProcessor
{
    public int Order => 3;
    
    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Ваша логика обработки
        await Task.CompletedTask;
    }
}
```

## Развертывание

### Локальное тестирование

Перед развертыванием на production сервер, можно протестировать Docker сборку локально:

**Linux/Mac:**
```bash
chmod +x test-docker.sh
./test-docker.sh
```

**Windows:**
```powershell
.\test-docker.ps1
```

Скрипт автоматически:
- 🔨 Собирает Docker образ
- 🚀 Запускает контейнер на порту 8001
- 🧪 Тестирует все эндпоинты
- 📊 Показывает логи и статистику
- 🧹 Очищает ресурсы после завершения

### Docker

Проект включает Dockerfile для контейнеризации приложения:

```bash
# Сборка образа
docker build -t webcorsproxy:latest .

# Запуск контейнера
docker run -d -p 8000:8080 --name proxyserver webcorsproxy:latest
```

Или с использованием docker-compose:

```bash
docker-compose up -d
```

### GitHub Actions (CI/CD)

Проект настроен для автоматического развертывания через GitHub Actions. При каждом push в ветку `main` происходит:

1. Сборка Docker образа
2. Передача образа на production сервер
3. Автоматический запуск нового контейнера

**🚀 Быстрый старт**: См. [.github/QUICKSTART.md](.github/QUICKSTART.md)

**📚 Подробные инструкции по настройке**: См. [.github/DEPLOYMENT.md](.github/DEPLOYMENT.md)

**Необходимые GitHub Secrets**:
- `SERVER_HOST` - IP адрес или домен сервера
- `SERVER_USER` - SSH пользователь
- `SSH_PRIVATE_KEY` - SSH приватный ключ
- `API_KEYS` - JSON массив с API ключами (например: `["key1", "key2"]`)
- `SERVER_PORT` - SSH порт (опционально, по умолчанию 22)
- `SIMPLE_CORS_REQUIRE_AUTH` - Требовать авторизацию для `/proxy` (опционально, по умолчанию `true`)

### Ручное развертывание на Linux сервере

```bash
# Клонирование репозитория
git clone https://github.com/AzerQ/WebCorsProxy.git
cd WebCorsProxy

# Сборка и запуск
docker build -t webcorsproxy:latest .
docker run -d \
  --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  webcorsproxy:latest
```

### Интеграция с Nginx

Пример конфигурации Nginx для проксирования на контейнер:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:8000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Преимущества новой архитектуры

1. **Гибкость** - возможность легко настраивать порядок выполнения обработчиков
2. **Расширяемость** - простое добавление новых обработчиков
3. **Конфигурируемость** - включение/отключение обработчиков через конфигурацию
4. **Разделение ответственности** - каждый обработчик отвечает за свою задачу
5. **Тестируемость** - возможность тестировать каждый обработчик отдельно