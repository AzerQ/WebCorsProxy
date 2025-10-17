# GitHub Actions Deployment Setup

Этот файл содержит инструкции по настройке автоматического развертывания через GitHub Actions.

## Варианты развертывания

Проект включает два варианта workflow для развертывания:

### 1. `deploy.yml` - Базовый вариант (рекомендуется для начала)
- ✅ Не требует дополнительных настроек
- ✅ Образ собирается и передается напрямую на сервер
- ✅ Проще для понимания и отладки
- ⚠️ Образ существует только на production сервере

### 2. `deploy-ghcr.yml` - Продвинутый вариант с GitHub Container Registry
- ✅ Образ хранится в GitHub Container Registry
- ✅ Возможность версионирования образов
- ✅ Легкий откат к предыдущим версиям
- ✅ Образ доступен из любого места
- ⚠️ Требует настройки прав доступа к packages

**Рекомендация**: Начните с `deploy.yml`. Если нужно версионирование и централизованное хранение образов, переключитесь на `deploy-ghcr.yml`.

Для использования варианта с GHCR удалите или переименуйте `deploy.yml` и переименуйте `deploy-ghcr.yml` в `deploy.yml`.

## Необходимые GitHub Secrets

Для работы автоматического развертывания необходимо настроить следующие секреты в вашем репозитории GitHub:

### Как добавить секреты:
1. Перейдите в ваш репозиторий на GitHub
2. Settings ➡️ Secrets and variables ➡️ Actions
3. Нажмите "New repository secret"
4. Добавьте каждый из секретов ниже

### Обязательные секреты:

#### `SERVER_HOST`
- **Описание**: IP адрес или доменное имя вашего сервера
- **Пример**: `192.168.1.100` или `server.example.com`

#### `SERVER_USER`
- **Описание**: Имя пользователя для SSH подключения
- **Пример**: `root` или `ubuntu`

#### `SERVER_PASSWORD`
- **Описание**: Пароль пользователя для SSH подключения
- **Пример**: `YourSecurePassword123!`
- **Важно**: Используется вместо SSH ключа для аутентификации. Убедитесь, что пароль достаточно сложный

#### `API_KEYS`
- **Описание**: JSON массив с API ключами для авторизации
- **Пример**: `["your-secret-key-1", "your-secret-key-2", "your-secret-key-3"]`
- **Важно**: 
  - Используйте длинные случайные строки (рекомендуется минимум 32 символа)
  - Можно сгенерировать: `openssl rand -hex 32`
  - Ключи используются для эндпоинтов `/web` и `/proxy` (если включена авторизация)

### Опциональные секреты:

#### `SERVER_PORT`
- **Описание**: SSH порт (по умолчанию 22)
- **Пример**: `22` или `2222`
- **Необязательно**: Если не указан, будет использоваться порт 22

#### `SIMPLE_CORS_REQUIRE_AUTH`
- **Описание**: Требовать ли авторизацию для эндпоинта `/proxy` (по умолчанию `true`)
- **Пример**: `true` или `false`
- **Необязательно**: Если не указан, будет использоваться значение `true`
- **Рекомендация**: Оставьте `true` для production, чтобы предотвратить злоупотребление прокси

## Настройка сервера

### 1. Установка Docker на сервере

```bash
# Обновите пакеты
sudo apt update

# Установите Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Добавьте пользователя в группу docker
sudo usermod -aG docker $USER

# Перелогиньтесь или выполните
newgrp docker

# Проверьте установку
docker --version
```

### 2. Создание директории для приложения

```bash
mkdir -p ~/webcorsproxy
```

### 3. Настройка Nginx (если еще не настроен)

Создайте конфигурацию для вашего сайта:

```bash
sudo nano /etc/nginx/sites-available/webcorsproxy
```

Пример конфигурации:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:8000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Увеличиваем таймауты для проксирования
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
        proxy_read_timeout 300;
    }
}
```

Активируйте конфигурацию:

```bash
sudo ln -s /etc/nginx/sites-available/webcorsproxy /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 4. Настройка HTTPS с Let's Encrypt (опционально)

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

## Workflow Configuration

### Триггеры развертывания:

1. **Автоматическое развертывание**: При каждом push в ветку `main`
2. **Ручное развертывание**: Через вкладку Actions в GitHub (кнопка "Run workflow")

### Что делает workflow:

1. ➡️ Проверяет код из репозитория
2. ⚙️ Собирает Docker образ
3. ⚙️ Сохраняет образ в архив
4. ⚙️ Копирует файлы на сервер
5. ⚙️ Загружает образ на сервере
6. ⚙️ Останавливает старый контейнер
7. ⚙️ Запускает новый контейнер
8. ⚙️ Очищает старые образы
9. ⚙️ Проверяет успешность развертывания

### Изменение настроек:

Если нужно изменить порт или имя контейнера, отредактируйте переменные в `.github/workflows/deploy.yml`:

```yaml
env:
  DOCKER_IMAGE_NAME: webcorsproxy    # Имя Docker образа
  CONTAINER_NAME: proxyserver        # Имя контейнера
```

## Проверка развертывания

### Локально (перед push):

```bash
# Соберите образ
docker build -t webcorsproxy:test .

# Запустите контейнер
docker run -d -p 8000:8080 --name test-proxy webcorsproxy:test

# Проверьте работу
curl http://localhost:8000/

# Остановите тестовый контейнер
docker stop test-proxy
docker rm test-proxy
```

### На сервере (после развертывания):

```bash
# Подключитесь к серверу
ssh user@server

# Проверьте статус контейнера
docker ps | grep proxyserver

# Посмотрите логи
docker logs proxyserver

# Посмотрите логи в реальном времени
docker logs -f proxyserver

# Проверьте работу приложения
curl http://localhost:8000/
```

## Troubleshooting

### Проблемы с SSH подключением:

```bash
# Проверьте SSH подключение локально
ssh -i ~/.ssh/your_key user@server

# Проверьте права на ключ
chmod 600 ~/.ssh/your_key
```

### Контейнер не запускается:

```bash
# Проверьте логи
docker logs proxyserver

# Проверьте, что порт свободен
sudo netstat -tlnp | grep 8000

# Запустите контейнер вручную для отладки
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

### Проблемы с файлом appsettings.json:

```bash
# Убедитесь, что файл существует на сервере
cat ~/webcorsproxy/appsettings.json

# Проверьте права доступа
chmod 644 ~/webcorsproxy/appsettings.json
```

## Ручное развертывание (без GitHub Actions)

Если нужно развернуть вручную:

```bash
# 1. Склонируйте репозиторий на сервере
git clone https://github.com/AzerQ/WebCorsProxy.git ~/webcorsproxy
cd ~/webcorsproxy

# 2. Соберите образ
docker build -t webcorsproxy:latest .

# 3. Остановите старый контейнер (если есть)
docker stop proxyserver || true
docker rm proxyserver || true

# 4. Запустите новый контейнер
docker run -d \
  --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  webcorsproxy:latest

# 5. Проверьте статус
docker ps | grep proxyserver
docker logs proxyserver
```

## Мониторинг

### Просмотр логов:

```bash
# Последние 100 строк
docker logs --tail 100 proxyserver

# В реальном времени
docker logs -f proxyserver

# С временными метками
docker logs -t proxyserver
```

### Статистика контейнера:

```bash
# Использование ресурсов
docker stats proxyserver

# Детальная информация
docker inspect proxyserver
```

## Откат к предыдущей версии

```bash
# Посмотрите доступные образы
docker images webcorsproxy

# Остановите текущий контейнер
docker stop proxyserver
docker rm proxyserver

# Запустите контейнер с предыдущим образом
docker run -d \
  --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  webcorsproxy:PREVIOUS_TAG
```
