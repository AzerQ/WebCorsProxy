# ������: ���������� API ������ ��� SimpleCorsProxy ����� GitHub Secrets

## ? ��� ���� �������:

### 1. **�������� SimpleCorsProxyService.cs**
- ��������� ��������� ������������ �����������
- ����� ����� �������� ����� �������� `token` ��� ��������� `Authorization: Bearer <token>`
- ����������� �������������� ����� ��������� `SimpleCorsProxy:RequireAuth`

### 2. **�������� Program.cs**
- �������� `/proxy` ������ ��������� ������������ �������� `token`
- ��������� ������������ Swagger

### 3. **��������� ���������������� �����**

**appsettings.json:**
```json
{
  "SimpleCorsProxy": {
    "RequireAuth": false  // Development - ��� �����������
  }
}
```

**appsettings.Production.json:**
```json
{
  "SimpleCorsProxy": {
    "RequireAuth": true  // Production - ������� �����������
  }
}
```

### 4. **��������� GitHub Actions Workflows**

��� workflow (`.github/workflows/deploy.yml` � `deploy-ghcr.yml`) ������:
- ������� `appsettings.Production.json` �� ������� �� GitHub Secrets
- ���������� �������:
  - `API_KEYS` - JSON ������ � API �������
  - `SIMPLE_CORS_REQUIRE_AUTH` - ��������/��������� ����������� ��� `/proxy`

### 5. **��������� ������������**

- **README.md**: ��������� ���������� �� ����������� ��� ����� ����������
- **.github/DEPLOYMENT.md**: ��������� ����� ������� `API_KEYS` � `SIMPLE_CORS_REQUIRE_AUTH`
- **.github/QUICKSTART.md**: �������� ������� � ������ ���������
- **.env.example**: ��������� ������� ����� ����������

## ?? ��������� GitHub Secrets:

### ������������ �������:

1. **API_KEYS**
   ```
   ������: ["key1", "key2", "key3"]
   ������: ["a1b2c3d4e5f6", "x9y8z7w6v5u4"]
   ```
   
   ��������� ���������� ������:
   ```bash
   openssl rand -hex 32
   ```

### ������������ �������:

2. **SIMPLE_CORS_REQUIRE_AUTH**
   ```
   ��������: true ��� false
   �� ���������: true
   ```

## ?? ������� �������������:

### Development (��� �����������):
```bash
curl "http://localhost:5000/proxy?url=https://api.example.com/data"
```

### Production (� ������������):

**����� �������� �������:**
```bash
curl "https://your-domain.com/proxy?url=https://api.example.com/data&token=your-api-key"
```

**����� ��������� Authorization:**
```bash
curl "https://your-domain.com/proxy?url=https://api.example.com/data" \
  -H "Authorization: Bearer your-api-key"
```

## ?? ������������:

? **������������**: API ����� �������� � GitHub Secrets, �� � �����������  
? **��������**: ����� ��������/��������� ����������� ��� `/proxy`  
? **�������������**: � development ������ �������� ��� �������  
? **��������**: ��������� ���� �������� �������� ������  

## ?? �����:

1. **�������** �� ��������� ���� `appsettings.Production.json` � ��������� ������� � �����������
2. ����������� ������� ��������� ����� (������� 32 �������)
3. � production ������ ��������� ����������� (`SimpleCorsProxy:RequireAuth = true`)
4. ��������� ������� API �����
5. ����������� HTTPS �� production

## ?? ������:

����� ��������� ��������:
```bash
git add .
git commit -m "feat: add API key auth for SimpleCorsProxy"
git push origin main
```

GitHub Actions �������������:
1. ������� `appsettings.Production.json` � ������ �������
2. ��������� ����������� ����������
3. ����� ���������� ����� ��������� ����������� (���� `SIMPLE_CORS_REQUIRE_AUTH=true`)

## ? FAQ:

**Q: ����� �� ������������ ������ ����� ��� `/web` � `/proxy`?**  
A: ���, ������������ ���� ������ `ApiKeys` ��� ����� ����������.

**Q: ��� ���� � ����, ����� `/proxy` ������� ��� ����������� �� production?**  
A: ���������� ������ `SIMPLE_CORS_REQUIRE_AUTH=false` � GitHub.

**Q: ��� �������� ����� ���� ��� ����������?**  
A: �������� ������ `API_KEYS` � GitHub � ��������� ����� ������.

**Q: �������� �� ������ ������� ��� ������?**  
A: ���� `SimpleCorsProxy:RequireAuth=false`, �� ��. ����� ����� 401 Unauthorized.

## ?? �������������� ���������:

- ������ ������������: [.github/DEPLOYMENT.md](.github/DEPLOYMENT.md)
- ������� �����: [.github/QUICKSTART.md](.github/QUICKSTART.md)
- ���������� �� ������� ����������: [MANUAL_UPDATE_INSTRUCTIONS.md](MANUAL_UPDATE_INSTRUCTIONS.md)
