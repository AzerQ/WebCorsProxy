# ?? WebCorsProxy - CI/CD Documentation

## ?? ����������

1. [������� �����](#�������-�����)
2. [��������� �������](#���������-�������)
3. [������� ������� ����������](#�������-�������-����������)
4. [�������������](#�������������)
5. [���������� � �������](#����������-�-�������)
6. [FAQ](#faq)

---

## ?? ������� �����

### ��� �������������

1. **���������� �����������**
   ```bash
   git clone https://github.com/AzerQ/WebCorsProxy.git
   cd WebCorsProxy
   ```

2. **��������� ����������**
   ```bash
   dotnet run
   # ���������� ���������� �� http://localhost:5000
   ```

3. **������������ Docker ��������**
   ```bash
   # Linux/Mac
   ./test-docker.sh
   
   # Windows
   .\test-docker.ps1
   ```

### ��� DevOps/���������������

1. **��������� GitHub Secrets** (��. [QUICKSTART.md](.github/QUICKSTART.md))
2. **��������� push � main** ? �������������� ������
3. **��������� ������** � GitHub Actions

---

## ?? ��������� �������

### �������� �����

```
WebCorsProxy/
??? .github/
?   ??? workflows/
?   ?   ??? deploy.yml              # �������� workflow (�������������)
?   ?   ??? deploy-ghcr.yml         # � GitHub Container Registry
?   ??? DEPLOYMENT.md               # ������ ������������ �� ������
?   ??? QUICKSTART.md               # ������� ����� (�������)
??? Pipelines/                      # ����������� ��������/�������
??? Configuration/                  # ������������ ����������
??? appsettings.json               # �������� ������������
??? appsettings.Production.json    # Production ������������
??? Dockerfile                      # Docker �����
??? docker-compose.yml             # Docker Compose ������������
??? test-docker.sh/ps1             # ��������� ������������
??? test-api.sh                    # API �����
??? README.md                      # �������� ������������
```

### ���������������� �����

- **appsettings.json** - �������� ��������� (��� dev)
- **appsettings.Production.json** - Production ���������
- **.env.example** - ������ ���������� ���������
- **docker-compose.yml** - ��������� Docker Compose

---

## ?? ������� ������� ����������

### 1. ���������� ����� �������

```bash
# �������� ����� ��� ����
git checkout -b feature/new-feature

# ������� ���������
# ... ��� ��� ...

# ��������� ������������
dotnet test
./test-docker.sh

# ������ � push
git add .
git commit -m "feat: add new feature"
git push origin feature/new-feature

# �������� Pull Request
```

### 2. ������������ ����� ������

- ? ��� �������������: `dotnet build`
- ? Docker ����� ����������: `./test-docker.sh`
- ? API ��������: `./test-api.sh`
- ? ��� ����� ��������: `dotnet test`

### 3. ���� � main

```bash
# ����� ��������� PR
git checkout main
git pull origin main
git merge feature/new-feature
git push origin main

# ?? ������������� ���������� ������!
```

---

## ?? �������������

### �������������� (����� GitHub Actions)

**��������:**
- Push � ����� `main`
- ������ ������ ����� Actions ? "Run workflow"

**�������:**
1. ? Checkout ����
2. ?? ������ Docker ������
3. ?? �������� ������
4. ?? �������� �� ������
5. ?? ��������� ������� ����������
6. ?? ������ ������ ����������
7. ?? �������� �������

**����������:**
- ������ � GitHub: Actions ? Deploy to Production
- ���� � �������� �������
- ����������� ��� �������

### ������ �������������

**�� �������:**
```bash
ssh user@server
cd ~/webcorsproxy

# �������� ��������� ���������
git pull origin main

# ������������ � ���������
docker-compose down
docker-compose up -d --build

# ��������� ������
docker-compose ps
docker-compose logs -f
```

### ����� ������

```bash
# ���������� ��������� ������
docker images webcorsproxy

# ��������� ���������� ������
docker stop proxyserver
docker rm proxyserver
docker run -d --name proxyserver \
  --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  webcorsproxy:PREVIOUS_IMAGE_ID
```

---

## ?? ���������� � �������

### �������� �������� ����������

```bash
# ������ ����������
docker ps | grep proxyserver

# ����
docker logs proxyserver
docker logs -f proxyserver  # � �������� �������

# �������
docker stats proxyserver

# ���� ����������
curl http://localhost:8000/
curl http://localhost:8000/proxy?url=https://example.com
```

### API ������������

```bash
# ��������
./test-api.sh

# �� production �������
SERVER_URL=https://your-domain.com API_TOKEN=your-token ./test-api.sh
```

### ������� �������

**��������� �� �����������:**
```bash
# ������ ����
docker logs proxyserver

# ������ � ������������� ������
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

**�������� � �������������:**
```bash
# ��������� ���� ��������
cat ~/webcorsproxy/appsettings.json

# ��������� ������������
docker exec proxyserver cat /app/appsettings.json
```

**Nginx �� ����� ������������:**
```bash
# ��������� ������ Nginx
sudo systemctl status nginx

# ��������� ������������
sudo nginx -t

# ���� Nginx
sudo tail -f /var/log/nginx/error.log
```

---

## ? FAQ

### Q: ��� �������� ���� ����������?
**A:** �������� � `docker-compose.yml`:
```yaml
ports:
  - "8000:8080"  # �������:����������
```

### Q: ��� �������� ����� API ����?
**A:** �������������� `appsettings.json` �� �������:
```json
{
  "ApiKeys": [
    "key-1",
    "key-2",
    "new-key"
  ]
}
```
����� �������������: `docker restart proxyserver`

### Q: ��� ����������, ����� ������ ����������?
**A:**
```bash
# ���� �������� ������
docker inspect proxyserver | grep Created

# ��� ����� GitHub Actions
# Actions ? Deploy to Production ? Latest run
```

### Q: ��� ���������� �� ������ ������?
**A:**
1. ��������� ��������� SSH �� ����� �������
2. �������� GitHub Secrets � ������ �������
3. ��������� ������

### Q: ����� �� ���������� ��������� �����������?
**A:** ��! �������� ���� � `docker-compose.yml`:
```yaml
services:
  proxyserver-1:
    ports:
      - "8000:8080"
  proxyserver-2:
    ports:
      - "8001:8080"
```

### Q: ��� �������� HTTPS?
**A:** ����������� Nginx � Let's Encrypt:
```bash
sudo certbot --nginx -d your-domain.com
```
��. [DEPLOYMENT.md](.github/DEPLOYMENT.md#���������-https)

### Q: Workflow ������ � ������� SSH
**A:** ���������:
1. ? SSH ���� �������� � GitHub Secrets
2. ? ��������� ���� �� ������� (`~/.ssh/authorized_keys`)
3. ? ����� �� ����: `chmod 600 ~/.ssh/authorized_keys`
4. ? SSH ���� ������ � firewall

### Q: ��� ��������� �������������� ������?
**A:** 
������� 1: ������� `.github/workflows/deploy.yml`
������� 2: �������� ������� � workflow:
```yaml
on:
  workflow_dispatch:  # ������ ������ ������
```

---

## ?? �������� ������

- [QUICKSTART.md](.github/QUICKSTART.md) - ������� �������
- [DEPLOYMENT.md](.github/DEPLOYMENT.md) - ��������� ������������
- [README.md](../README.md) - �������� �������
- [GitHub Actions](https://github.com/AzerQ/WebCorsProxy/actions) - ������ �������

---

## ?? �������� � ���������

- **Issues**: https://github.com/AzerQ/WebCorsProxy/issues
- **Pull Requests**: https://github.com/AzerQ/WebCorsProxy/pulls
- **Discussions**: https://github.com/AzerQ/WebCorsProxy/discussions

---

**��������� ����������**: 2024
**������ ������������**: 1.0
