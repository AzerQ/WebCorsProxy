# HTTP Proxy Server with Configurable Pipeline

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
   - `AuthorizationProcessor` - проверка авторизации по API ключам
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
        "AuthorizationProcessor",
        "HeadersProcessor"
      ],
      "ProcessorSettings": {
        "ValidationProcessor": {
          "Order": 0,
          "Enabled": true
        },
        "AuthorizationProcessor": {
          "Order": 1,
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

- `/web?url={url}&token={token}` - основной эндпоинт с новым пайплайном
- `/web/legacy?url={url}&token={token}` - эндпоинт со старой реализацией

### Пример запроса

```
GET /web?url=https://example.com&token=your-api-key-1
```

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

## Преимущества новой архитектуры

1. **Гибкость** - возможность легко настраивать порядок выполнения обработчиков
2. **Расширяемость** - простое добавление новых обработчиков
3. **Конфигурируемость** - включение/отключение обработчиков через конфигурацию
4. **Разделение ответственности** - каждый обработчик отвечает за свою задачу
5. **Тестируемость** - возможность тестировать каждый обработчик отдельно