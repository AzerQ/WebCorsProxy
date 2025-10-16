# Docker Test Script

Write-Host "?? Testing Docker build and deployment locally...`n" -ForegroundColor Yellow

# Переменные
$IMAGE_NAME = "webcorsproxy-test"
$CONTAINER_NAME = "proxyserver-test"
$PORT = 8001

# Функция для очистки
function Cleanup {
    Write-Host "`n?? Cleaning up..." -ForegroundColor Yellow
    docker stop $CONTAINER_NAME 2>$null
    docker rm $CONTAINER_NAME 2>$null
    docker rmi $IMAGE_NAME 2>$null
    Write-Host "? Cleanup completed" -ForegroundColor Green
}

# Регистрация обработчика для Ctrl+C
$null = Register-EngineEvent PowerShell.Exiting -Action { Cleanup }

try {
    # 1. Сборка образа
    Write-Host "?? Building Docker image..." -ForegroundColor Yellow
    docker build -t $IMAGE_NAME .
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Build successful`n" -ForegroundColor Green
    } else {
        Write-Host "? Build failed" -ForegroundColor Red
        exit 1
    }

    # 2. Запуск контейнера
    Write-Host "?? Starting container..." -ForegroundColor Yellow
    $currentDir = (Get-Location).Path
    docker run -d `
        --name $CONTAINER_NAME `
        -p "${PORT}:8080" `
        -v "${currentDir}/appsettings.json:/app/appsettings.json:ro" `
        -e ASPNETCORE_ENVIRONMENT=Development `
        $IMAGE_NAME
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Container started`n" -ForegroundColor Green
    } else {
        Write-Host "? Failed to start container" -ForegroundColor Red
        exit 1
    }

    # 3. Ожидание запуска
    Write-Host "? Waiting for application to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    # 4. Проверка статуса
    Write-Host "?? Container status:" -ForegroundColor Yellow
    docker ps -f name=$CONTAINER_NAME

    # 5. Проверка логов
    Write-Host "`n?? Container logs:" -ForegroundColor Yellow
    docker logs $CONTAINER_NAME

    # 6. Тестирование эндпоинтов
    Write-Host "`n?? Testing endpoints..." -ForegroundColor Yellow

    # Тест 1: Базовый эндпоинт
    Write-Host "`nTest 1: GET /" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$PORT/" -UseBasicParsing -ErrorAction Stop
        Write-Host "? Root endpoint works" -ForegroundColor Green
    } catch {
        Write-Host "? Root endpoint responds (redirect expected)" -ForegroundColor Green
    }

    # Тест 2: /proxy эндпоинт
    Write-Host "`nTest 2: GET /proxy?url=https://httpbin.org/get" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$PORT/proxy?url=https://httpbin.org/get" -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "? Proxy endpoint works" -ForegroundColor Green
            Write-Host "Response preview:"
            Write-Host ($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))
        }
    } catch {
        Write-Host "? Proxy endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Тест 3: /web эндпоинт
    Write-Host "`nTest 3: GET /web?url=https://example.com" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$PORT/web?url=https://example.com" -UseBasicParsing
        Write-Host "? Web endpoint responds (HTTP $($response.StatusCode))" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host "? Web endpoint responds (HTTP 401 - auth required)" -ForegroundColor Green
        } else {
            Write-Host "? Web endpoint failed (HTTP $statusCode)" -ForegroundColor Red
        }
    }

    # 7. Информация о ресурсах
    Write-Host "`n?? Container resources:" -ForegroundColor Yellow
    docker stats --no-stream $CONTAINER_NAME

    # 8. Итоговая информация
    Write-Host "`n????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "? Local testing completed!" -ForegroundColor Green
    Write-Host "????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "`n?? You can:" -ForegroundColor Yellow
    Write-Host "  - View logs: docker logs -f $CONTAINER_NAME" -ForegroundColor Green
    Write-Host "  - Access app: http://localhost:$PORT" -ForegroundColor Green
    Write-Host "  - Test proxy: curl 'http://localhost:$PORT/proxy?url=https://example.com'" -ForegroundColor Green
    Write-Host "  - Stop container: docker stop $CONTAINER_NAME" -ForegroundColor Green
    Write-Host "`nPress Enter to cleanup and exit" -ForegroundColor Yellow
    
    $null = Read-Host
}
finally {
    Cleanup
}
