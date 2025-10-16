# ?? WebCorsProxy - CI/CD Documentation

## ?? Оглавление

1. [Быстрый старт](#быстрый-старт)
2. [Структура проекта](#структура-проекта)
3. [Рабочий процесс разработки](#рабочий-процесс-разработки)
4. [Развертывание](#развертывание)
5. [Мониторинг и отладка](#мониторинг-и-отладка)
6. [FAQ](#faq)

---

## ?? Быстрый старт

### Для разработчиков

1. **Клонируйте репозиторий**
   ```bash
   git clone https://github.com/AzerQ/WebCorsProxy.git
   cd WebCorsProxy
   ```

2. **Локальная разработка**
   ```bash
   dotnet run
   # Приложение запустится на http://localhost:5000
   ```

3. **Тестирование Docker локально**
   ```bash
   # Linux/Mac
   ./test-docker.sh
   
   # Windows
   .\test-docker.ps1
   ```

### Для DevOps/Администраторов

1. **Настройте GitHub Secrets** (см. [QUICKSTART.md](.github/QUICKSTART.md))
2. **Выполните push в main** ? автоматический деплой
3. **Проверьте статус** в GitHub Actions

---

## ?? Структура проекта

### Основные файлы

```
WebCorsProxy/
??? .github/
?   ??? workflows/
?   ?   ??? deploy.yml              # Основной workflow (рекомендуется)
?   ?   ??? deploy-ghcr.yml         # С GitHub Container Registry
?   ??? DEPLOYMENT.md               # Полная документация по деплою
?   ??? QUICKSTART.md               # Быстрый старт (чеклист)
??? Pipelines/                      # Обработчики запросов/ответов
??? Configuration/                  # Конфигурации пайплайнов
??? appsettings.json               # Основная конфигурация
??? appsettings.Production.json    # Production конфигурация
??? Dockerfile                      # Docker образ
??? docker-compose.yml             # Docker Compose конфигурация
??? test-docker.sh/ps1             # Локальное тестирование
??? test-api.sh                    # API тесты
??? README.md                      # Основная документация
```

### Конфигурационные файлы

- **appsettings.json** - Основные настройки (для dev)
- **appsettings.Production.json** - Production настройки
- **.env.example** - Пример переменных окружения
- **docker-compose.yml** - Настройки Docker Compose

---

## ?? Рабочий процесс разработки

### 1. Разработка новой функции

```bash
# Создайте ветку для фичи
git checkout -b feature/new-feature

# Внесите изменения
# ... ваш код ...

# Локальное тестирование
dotnet test
./test-docker.sh

# Коммит и push
git add .
git commit -m "feat: add new feature"
git push origin feature/new-feature

# Создайте Pull Request
```

### 2. Тестирование перед мержем

- ? Код компилируется: `dotnet build`
- ? Docker образ собирается: `./test-docker.sh`
- ? API работает: `./test-api.sh`
- ? Все тесты проходят: `dotnet test`

### 3. Мерж в main

```bash
# После одобрения PR
git checkout main
git pull origin main
git merge feature/new-feature
git push origin main

# ?? Автоматически запустится деплой!
```

---

## ?? Развертывание

### Автоматическое (через GitHub Actions)

**Триггеры:**
- Push в ветку `main`
- Ручной запуск через Actions ? "Run workflow"

**Процесс:**
1. ? Checkout кода
2. ?? Сборка Docker образа
3. ?? Упаковка образа
4. ?? Передача на сервер
5. ?? Остановка старого контейнера
6. ?? Запуск нового контейнера
7. ?? Проверка статуса

**Мониторинг:**
- Статус в GitHub: Actions ? Deploy to Production
- Логи в реальном времени
- Уведомления при ошибках

### Ручное развертывание

**На сервере:**
```bash
ssh user@server
cd ~/webcorsproxy

# Получите последние изменения
git pull origin main

# Пересоберите и запустите
docker-compose down
docker-compose up -d --build

# Проверьте статус
docker-compose ps
docker-compose logs -f
```

### Откат версии

```bash
# Посмотрите доступные образы
docker images webcorsproxy

# Запустите предыдущую версию
docker stop proxyserver
docker rm proxyserver
docker run -d --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  webcorsproxy:PREVIOUS_IMAGE_ID
```

---

## ?? Мониторинг и отладка

### Проверка здоровья приложения

```bash
# Статус контейнера
docker ps | grep proxyserver

# Логи
docker logs proxyserver
docker logs -f proxyserver  # В реальном времени

# Ресурсы
docker stats proxyserver

# Тест эндпоинтов
curl http://localhost:8000/
curl http://localhost:8000/proxy?url=https://example.com
```

### API тестирование

```bash
# Локально
./test-api.sh

# На production сервере
SERVER_URL=https://your-domain.com API_TOKEN=your-token ./test-api.sh
```

### Отладка проблем

**Контейнер не запускается:**
```bash
# Полные логи
docker logs proxyserver

# Запуск в интерактивном режиме
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

**Проблемы с конфигурацией:**
```bash
# Проверьте файл настроек
cat ~/webcorsproxy/appsettings.json

# Проверьте монтирование
docker exec proxyserver cat /app/appsettings.json
```

**Nginx не может подключиться:**
```bash
# Проверьте статус Nginx
sudo systemctl status nginx

# Проверьте конфигурацию
sudo nginx -t

# Логи Nginx
sudo tail -f /var/log/nginx/error.log
```

---

## ? FAQ

### Q: Как изменить порт приложения?
**A:** Измените в `docker-compose.yml`:
```yaml
ports:
  - "8000:8080"  # Внешний:Внутренний
```

### Q: Как добавить новый API ключ?
**A:** Отредактируйте `appsettings.json` на сервере:
```json
{
  "ApiKeys": [
    "key-1",
    "key-2",
    "new-key"
  ]
}
```
Затем перезапустите: `docker restart proxyserver`

### Q: Как посмотреть, какая версия развернута?
**A:**
```bash
# Дата создания образа
docker inspect proxyserver | grep Created

# Или через GitHub Actions
# Actions ? Deploy to Production ? Latest run
```

### Q: Как развернуть на другой сервер?
**A:**
1. Повторите настройку SSH на новом сервере
2. Обновите GitHub Secrets с новыми данными
3. Выполните деплой

### Q: Можно ли развернуть несколько экземпляров?
**A:** Да! Измените порт в `docker-compose.yml`:
```yaml
services:
  proxyserver-1:
    ports:
      - "8000:8080"
  proxyserver-2:
    ports:
      - "8001:8080"
```

### Q: Как включить HTTPS?
**A:** Используйте Nginx с Let's Encrypt:
```bash
sudo certbot --nginx -d your-domain.com
```
См. [DEPLOYMENT.md](.github/DEPLOYMENT.md#настройка-https)

### Q: Workflow падает с ошибкой SSH
**A:** Проверьте:
1. ? SSH ключ добавлен в GitHub Secrets
2. ? Публичный ключ на сервере (`~/.ssh/authorized_keys`)
3. ? Права на ключ: `chmod 600 ~/.ssh/authorized_keys`
4. ? SSH порт открыт в firewall

### Q: Как отключить автоматический деплой?
**A:** 
Вариант 1: Удалите `.github/workflows/deploy.yml`
Вариант 2: Измените триггер в workflow:
```yaml
on:
  workflow_dispatch:  # Только ручной запуск
```

---

## ?? Полезные ссылки

- [QUICKSTART.md](.github/QUICKSTART.md) - Быстрый чеклист
- [DEPLOYMENT.md](.github/DEPLOYMENT.md) - Детальная документация
- [README.md](../README.md) - Описание проекта
- [GitHub Actions](https://github.com/AzerQ/WebCorsProxy/actions) - Статус деплоев

---

## ?? Контакты и поддержка

- **Issues**: https://github.com/AzerQ/WebCorsProxy/issues
- **Pull Requests**: https://github.com/AzerQ/WebCorsProxy/pulls
- **Discussions**: https://github.com/AzerQ/WebCorsProxy/discussions

---

**Последнее обновление**: 2024
**Версия документации**: 1.0
