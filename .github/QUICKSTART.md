# ?? Быстрый старт развертывания

## ?? Checklist для развертывания

### 1. Подготовка сервера
- [ ] Установлен Docker
- [ ] Установлен и настроен Nginx
- [ ] Создана директория `~/webcorsproxy`
- [ ] Скопирован файл `appsettings.json` (с вашими API ключами)

### 2. Настройка SSH доступа
- [ ] Создан SSH ключ для GitHub Actions
- [ ] Публичный ключ добавлен на сервер (`~/.ssh/authorized_keys`)
- [ ] Проверено подключение: `ssh -i key user@server`

### 3. Настройка GitHub Secrets
Добавьте следующие секреты в Settings ? Secrets and variables ? Actions:

#### Обязательные:
- [ ] `SERVER_HOST` (например: `192.168.1.100`)
- [ ] `SERVER_USER` (например: `ubuntu`)
- [ ] `SERVER_PASSWORD` (пароль для SSH подключения)
- [ ] `API_KEYS` (JSON массив: `["key1", "key2"]`)

#### Опциональные:
- [ ] `SERVER_PORT` (по умолчанию 22)
- [ ] `SIMPLE_CORS_REQUIRE_AUTH` (по умолчанию `true`)

**?? Генерация безопасных API ключей:**
```bash
# Сгенерируйте случайные ключи
openssl rand -hex 32  # key1
openssl rand -hex 32  # key2

# Затем добавьте в GitHub Secret API_KEYS:
["your-generated-key-1", "your-generated-key-2"]
```

### 4. Проверка workflow
- [ ] Файл `.github/workflows/deploy.yml` существует
- [ ] Workflow запускается при push в `main`
- [ ] Можно запустить вручную через Actions ? Run workflow

### 5. Первое развертывание
```bash
# Убедитесь, что все изменения закоммичены
git add .
git commit -m "Setup CI/CD"
git push origin main

# Или запустите вручную через GitHub Actions
```

### 6. Проверка после развертывания
```bash
# Подключитесь к серверу
ssh user@server

# Проверьте статус контейнера
docker ps | grep proxyserver

# Проверьте логи
docker logs proxyserver

# Проверьте работу через curl
curl http://localhost:8000/

# Проверьте через Nginx
curl http://your-domain.com/
```

## ?? Быстрая отладка

### Workflow не запускается?
1. Проверьте вкладку Actions в GitHub
2. Убедитесь, что push был в ветку `main`
3. Проверьте, что файл `.github/workflows/deploy.yml` есть в репозитории

### Ошибка SSH подключения?
```bash
# Проверьте на локальной машине
ssh -i ~/.ssh/your_key user@server

# Проверьте права на ключ
chmod 600 ~/.ssh/your_key

# Проверьте, что публичный ключ добавлен на сервер
cat ~/.ssh/authorized_keys
```

### Контейнер не запускается?
```bash
# Посмотрите полные логи
docker logs proxyserver

# Проверьте, что порт свободен
sudo netstat -tlnp | grep 8000

# Попробуйте запустить вручную
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

### Nginx не может подключиться?
```bash
# Проверьте статус Nginx
sudo systemctl status nginx

# Проверьте конфигурацию
sudo nginx -t

# Проверьте логи Nginx
sudo tail -f /var/log/nginx/error.log
```

## ?? Полезные команды

### На сервере:
```bash
# Посмотреть логи в реальном времени
docker logs -f proxyserver

# Перезапустить контейнер
docker restart proxyserver

# Остановить контейнер
docker stop proxyserver

# Удалить контейнер
docker rm proxyserver

# Посмотреть использование ресурсов
docker stats proxyserver

# Подключиться к контейнеру
docker exec -it proxyserver /bin/bash
```

### Локально:
```bash
# Тестовая сборка
docker build -t test .

# Локальный запуск
docker run -it --rm -p 8000:8080 test

# Проверка эндпоинтов
curl "http://localhost:8000/proxy?url=https://example.com"
```

## ?? Обновление после развертывания

После успешного первого развертывания, последующие обновления происходят автоматически:

1. Внесите изменения в код
2. Закоммитьте: `git commit -am "Your changes"`
3. Запушьте: `git push origin main`
4. GitHub Actions автоматически развернет новую версию

## ?? Откат версии

```bash
# Посмотрите доступные образы
docker images webcorsproxy

# Запустите предыдущую версию
docker stop proxyserver
docker rm proxyserver
docker run -d --name proxyserver --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  webcorsproxy:PREVIOUS_IMAGE_ID
```

## ?? Дополнительная документация

- Полная документация: [DEPLOYMENT.md](DEPLOYMENT.md)
- Основной README: [../README.md](../README.md)
- Конфигурация Nginx: [DEPLOYMENT.md#настройка-nginx](DEPLOYMENT.md#3-настройка-nginx-если-еще-не-настроен)
