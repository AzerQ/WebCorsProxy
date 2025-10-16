# ?? ������� ����� �������������

## ?? Checklist ��� �������������

### 1. ���������� �������
- [ ] ���������� Docker
- [ ] ���������� � �������� Nginx
- [ ] ������� ���������� `~/webcorsproxy`
- [ ] ���������� ���� `appsettings.json` (� ������ API �������)

### 2. ��������� SSH �������
- [ ] ������ SSH ���� ��� GitHub Actions
- [ ] ��������� ���� �������� �� ������ (`~/.ssh/authorized_keys`)
- [ ] ��������� �����������: `ssh -i key user@server`

### 3. ��������� GitHub Secrets
�������� ��������� ������� � Settings ? Secrets and variables ? Actions:

#### ������������:
- [ ] `SERVER_HOST` (��������: `192.168.1.100`)
- [ ] `SERVER_USER` (��������: `ubuntu`)
- [ ] `SERVER_PASSWORD` (������ ��� SSH �����������)
- [ ] `API_KEYS` (JSON ������: `["key1", "key2"]`)

#### ������������:
- [ ] `SERVER_PORT` (�� ��������� 22)
- [ ] `SIMPLE_CORS_REQUIRE_AUTH` (�� ��������� `true`)

**?? ��������� ���������� API ������:**
```bash
# ������������ ��������� �����
openssl rand -hex 32  # key1
openssl rand -hex 32  # key2

# ����� �������� � GitHub Secret API_KEYS:
["your-generated-key-1", "your-generated-key-2"]
```

### 4. �������� workflow
- [ ] ���� `.github/workflows/deploy.yml` ����������
- [ ] Workflow ����������� ��� push � `main`
- [ ] ����� ��������� ������� ����� Actions ? Run workflow

### 5. ������ �������������
```bash
# ���������, ��� ��� ��������� �����������
git add .
git commit -m "Setup CI/CD"
git push origin main

# ��� ��������� ������� ����� GitHub Actions
```

### 6. �������� ����� �������������
```bash
# ������������ � �������
ssh user@server

# ��������� ������ ����������
docker ps | grep proxyserver

# ��������� ����
docker logs proxyserver

# ��������� ������ ����� curl
curl http://localhost:8000/

# ��������� ����� Nginx
curl http://your-domain.com/
```

## ?? ������� �������

### Workflow �� �����������?
1. ��������� ������� Actions � GitHub
2. ���������, ��� push ��� � ����� `main`
3. ���������, ��� ���� `.github/workflows/deploy.yml` ���� � �����������

### ������ SSH �����������?
```bash
# ��������� �� ��������� ������
ssh -i ~/.ssh/your_key user@server

# ��������� ����� �� ����
chmod 600 ~/.ssh/your_key

# ���������, ��� ��������� ���� �������� �� ������
cat ~/.ssh/authorized_keys
```

### ��������� �� �����������?
```bash
# ���������� ������ ����
docker logs proxyserver

# ���������, ��� ���� ��������
sudo netstat -tlnp | grep 8000

# ���������� ��������� �������
docker run -it --rm -p 8000:8080 webcorsproxy:latest
```

### Nginx �� ����� ������������?
```bash
# ��������� ������ Nginx
sudo systemctl status nginx

# ��������� ������������
sudo nginx -t

# ��������� ���� Nginx
sudo tail -f /var/log/nginx/error.log
```

## ?? �������� �������

### �� �������:
```bash
# ���������� ���� � �������� �������
docker logs -f proxyserver

# ������������� ���������
docker restart proxyserver

# ���������� ���������
docker stop proxyserver

# ������� ���������
docker rm proxyserver

# ���������� ������������� ��������
docker stats proxyserver

# ������������ � ����������
docker exec -it proxyserver /bin/bash
```

### ��������:
```bash
# �������� ������
docker build -t test .

# ��������� ������
docker run -it --rm -p 8000:8080 test

# �������� ����������
curl "http://localhost:8000/proxy?url=https://example.com"
```

## ?? ���������� ����� �������������

����� ��������� ������� �������������, ����������� ���������� ���������� �������������:

1. ������� ��������� � ���
2. �����������: `git commit -am "Your changes"`
3. ��������: `git push origin main`
4. GitHub Actions ������������� ��������� ����� ������

## ?? ����� ������

```bash
# ���������� ��������� ������
docker images webcorsproxy

# ��������� ���������� ������
docker stop proxyserver
docker rm proxyserver
docker run -d --name proxyserver --restart unless-stopped \
  -p 8000:8080 \
  -v ~/webcorsproxy/appsettings.json:/app/appsettings.json:ro \
  webcorsproxy:PREVIOUS_IMAGE_ID
```

## ?? �������������� ������������

- ������ ������������: [DEPLOYMENT.md](DEPLOYMENT.md)
- �������� README: [../README.md](../README.md)
- ������������ Nginx: [DEPLOYMENT.md#���������-nginx](DEPLOYMENT.md#3-���������-nginx-����-���-��-��������)
