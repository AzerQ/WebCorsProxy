# GitHub Actions Deployment Setup

���� ���� �������� ���������� �� ��������� ��������������� ������������� ����� GitHub Actions.

## �������� �������������

������ �������� ��� �������� workflow ��� �������������:

### 1. `deploy.yml` - ������� ������� (������������� ��� ������)
- ? �� ������� �������������� ��������
- ? ����� ���������� � ���������� �������� �� ������
- ? ����� ��� ��������� � �������
- ?? ����� ���������� ������ �� production �������

### 2. `deploy-ghcr.yml` - ����������� ������� � GitHub Container Registry
- ? ����� �������� � GitHub Container Registry
- ? ����������� ��������������� �������
- ? ������ ����� � ���������� �������
- ? ����� �������� �� ������ �����
- ?? ������� ��������� ���� ������� � packages

**������������**: ������� � `deploy.yml`. ���� ����� ��������������� � ���������������� �������� �������, ������������� �� `deploy-ghcr.yml`.

��� ������������� �������� � GHCR ������� ��� ������������ `deploy.yml` � ������������ `deploy-ghcr.yml` � `deploy.yml`.

## ����������� GitHub Secrets

��� ������ ��������������� ������������� ���������� ��������� ��������� ������� � ����� ����������� GitHub:

### ��� �������� �������:
1. ��������� � ��� ����������� �� GitHub
2. Settings ? Secrets and variables ? Actions
3. ������� "New repository secret"
4. �������� ������ �� �������� ����

### ������������ �������:

#### `SERVER_HOST`
- **��������**: IP ����� ��� �������� ��� ������ �������
- **������**: `192.168.1.100` ��� `server.example.com`

#### `SERVER_USER`
- **��������**: ��� ������������ ��� SSH �����������
- **������**: `root` ��� `ubuntu`

#### `SERVER_PASSWORD`
- **��������**: ������ ������������ ��� SSH �����������
- **������**: `YourSecurePassword123!`
- **�����**: ������������ ������ SSH ����� ��� ��������������. ���������, ��� ������ ���������� �������

#### `API_KEYS`
- **��������**: JSON ������ � API ������� ��� �����������
- **������**: `["your-secret-key-1", "your-secret-key-2", "your-secret-key-3"]`
- **�����**: 
  - ����������� ������� ��������� ������ (������������� ������� 32 �������)
  - ����� �������������: `openssl rand -hex 32`
  - ����� ������������ ��� ���������� `/web` � `/proxy` (���� �������� �����������)

### ������������ �������:

#### `SERVER_PORT`
- **��������**: SSH ���� (�� ��������� 22)
- **������**: `22` ��� `2222`
- **�������������**: ���� �� ������, ����� �������������� ���� 22

#### `SIMPLE_CORS_REQUIRE_AUTH`
- **��������**: ��������� �� ����������� ��� ��������� `/proxy` (�� ��������� `true`)
- **������**: `true` ��� `false`
- **�������������**: ���� �� ������, ����� �������������� �������� `true`
- **������������**: �������� `true` ��� production, ����� ������������� ��������������� ������

## ��������� �������

### 1. ��������� Docker �� �������

```bash
# �������� ������
sudo apt update

# ���������� Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# �������� ������������ � ������ docker
sudo usermod -aG docker $USER

# �������������� ��� ���������
newgrp docker

# ��������� ���������
docker --version
```

### 2. �������� ���������� ��� ����������

```bash
mkdir -p ~/webcorsproxy
```

### 3. ��������� Nginx (���� ��� �� ��������)

�������� ������������ ��� ������ �����:

```bash
sudo nano /etc/nginx/sites-available/webcorsproxy
```

������ ������������:

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
        
        # ����������� �������� ��� �������������
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
        proxy_read_timeout 300;
    }
}
```

����������� ������������:

```bash
sudo ln -s /etc/nginx/sites-available/webcorsproxy /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 4. ��������� HTTPS � Let's Encrypt (�����������)

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

## Workflow Configuration

### �������� �������������:

1. **�������������� �������������**: ��� ������ push � ����� `main`
2. **������ �������������**: ����� ������� Actions � GitHub (������ "Run workflow")

### ��� ������ workflow:

1. ? ��������� ��� �� �����������
2. ?? �������� Docker �����
3. ?? ��������� ����� � �����
4. ?? �������� ����� �� ������
5. ?? ��������� ����� �� �������
6. ?? ������������� ������ ���������
7. ?? ��������� ����� ���������
8. ?? ������� ������ ������
9. ?? ��������� ���������� �������������

### ��������� ��������:

���� ����� �������� ���� ��� ��� ����������, �������������� ���������� � `.github/workflows/deploy.yml`:

```yaml
env:
  DOCKER_IMAGE_NAME: webcorsproxy    # ��� Docker ������
  CONTAINER_NAME: proxyserver        # ��� ����������
```

## �������� �������������

### �������� (����� push):

```bash
# �������� �����
docker build -t webcorsproxy:test .

# ��������� ���������
docker run -d -p 8000:8080 --name test-proxy webcorsproxy:test

# ��������� ������
curl http://localhost:8000/

# ���������� �������� ���������
docker stop test-proxy
docker rm test-proxy
```

### �� ������� (����� �������������):

```bash
# ������������ � �������
ssh user@server

# ��������� ������ ����������
docker ps | grep proxyserver

# ���������� ����
docker logs proxyserver

# ���������� ���� � �������� �������
docker logs -f proxyserver

# ��������� ������ ����������
curl http://localhost:8000/
```

## Troubleshooting

### �������� � SSH ������������:

```bash
# ��������� SSH ����������� ��������
ssh -i ~/.ssh/your_key user@server

# ��������� ����� �� ����
chmod 600 ~/.ssh/your_key
```

### ��������� �� �����������:

```bash
# ��������� ����
docker logs proxyserver

# ���������, ��� ���� ��������
sudo netstat -tlnp | grep 8000

# ��������� ��������� ������� ��� �������
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

### �������� � ������ appsettings.json:

```bash
# ���������, ��� ���� ���������� �� �������
cat ~/webcorsproxy/appsettings.json

# ��������� ����� �������
chmod 644 ~/webcorsproxy/appsettings.json
```

## ������ ������������� (��� GitHub Actions)

���� ����� ���������� �������:

```bash
# 1. ����������� ����������� �� �������
git clone https://github.com/AzerQ/WebCorsProxy.git ~/webcorsproxy
cd ~/webcorsproxy

# 2. �������� �����
docker build -t webcorsproxy:latest .

# 3. ���������� ������ ��������� (���� ����)
docker stop proxyserver || true
docker rm proxyserver || true

# 4. ��������� ����� ���������
docker run -d \
  --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  webcorsproxy:latest

# 5. ��������� ������
docker ps | grep proxyserver
docker logs proxyserver
```

## ����������

### �������� �����:

```bash
# ��������� 100 �����
docker logs --tail 100 proxyserver

# � �������� �������
docker logs -f proxyserver

# � ���������� �������
docker logs -t proxyserver
```

### ���������� ����������:

```bash
# ������������� ��������
docker stats proxyserver

# ��������� ����������
docker inspect proxyserver
```

## ����� � ���������� ������

```bash
# ���������� ��������� ������
docker images webcorsproxy

# ���������� ������� ���������
docker stop proxyserver
docker rm proxyserver

# ��������� ��������� � ���������� �������
docker run -d \
  --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  webcorsproxy:PREVIOUS_TAG
```
