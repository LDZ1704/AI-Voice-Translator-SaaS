# Hướng dẫn Deploy AI Voice Translator SaaS lên Azure

Hướng dẫn chi tiết để deploy ứng dụng ASP.NET Core 8.0 lên Azure App Service với các Azure services tích hợp.

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Chuẩn bị](#chuẩn-bị)
3. [Deploy Azure App Service](#deploy-azure-app-service)
4. [Cấu hình Azure SQL Database](#cấu-hình-azure-sql-database)
5. [Cấu hình Azure Redis Cache](#cấu-hình-azure-redis-cache)
6. [Cấu hình Application Settings](#cấu-hình-application-settings)
7. [Deploy Database Migrations](#deploy-database-migrations)
8. [Cấu hình Azure Services](#cấu-hình-azure-services)
9. [Cấu hình Custom Domain & SSL](#cấu-hình-custom-domain--ssl)
10. [Cấu hình OAuth Providers](#cấu-hình-oauth-providers)
11. [Cấu hình MoMo Payment](#cấu-hình-momo-payment)
12. [Monitoring & Troubleshooting](#monitoring--troubleshooting)

---

## Tổng quan

Hệ thống cần các Azure resources sau:
- **Azure App Service** (Web App) - Host ứng dụng
- **Azure SQL Database** - Database chính
- **Azure Redis Cache** - Caching layer
- **Azure Speech Service** - STT/TTS
- **Azure Translator** - Translation API
- **Azure Blob Storage** - File storage
- **Azure Key Vault** (Optional) - Secrets management

---

## Chuẩn bị

### 1. Yêu cầu

- Azure Subscription (có thể dùng Free Trial)
- Azure CLI hoặc Azure PowerShell đã cài đặt
- .NET 8 SDK trên máy local
- Git đã cài đặt
- Visual Studio 2022 hoặc VS Code (optional)

### 2. Đăng nhập Azure

```bash
# Đăng nhập Azure CLI
az login

# Kiểm tra subscription
az account show

# Nếu có nhiều subscriptions, chọn subscription cần dùng
az account set --subscription "Your Subscription Name"
```

---

## Deploy Azure App Service

### 1. Tạo Resource Group

```bash
az group create \
  --name rg-ai-voice-translator \
  --location southeastasia
```

### 2. Tạo App Service Plan

```bash
# Tạo App Service Plan (Basic B1 - $13/tháng, hoặc Free F1 cho test)
az appservice plan create \
  --name plan-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --sku B1 \
  --is-linux
```

**Lưu ý**: 
- **Free (F1)**: 1 GB storage, 60 phút CPU/day, không có custom domain
- **Basic (B1)**: 10 GB storage, không giới hạn CPU, hỗ trợ custom domain
- **Standard (S1)**: Recommended cho production

### 3. Tạo Web App

```bash
az webapp create \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --plan plan-ai-voice-translator \
  --runtime "DOTNET|8.0"
```

### 4. Cấu hình Always On (quan trọng cho Hangfire)

```bash
az webapp config set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --always-on true
```

### 5. Build và Deploy từ Local

#### Option A: Deploy từ Visual Studio

1. Right-click project → **Publish**
2. Chọn **Azure** → **Azure App Service (Linux)**
3. Chọn App Service đã tạo
4. Click **Publish**

#### Option B: Deploy từ Command Line

```bash
# Build project
cd src
dotnet publish -c Release -o ./publish

# Deploy bằng Azure CLI
az webapp deployment source config-zip \
  --resource-group rg-ai-voice-translator \
  --name ai-voice-translator-app \
  --src ./publish.zip
```

#### Option C: Deploy từ GitHub Actions (CI/CD)

Tạo file `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: dotnet build src/AI-Voice-Translator-SaaS.csproj --configuration Release
    
    - name: Publish
      run: dotnet publish src/AI-Voice-Translator-SaaS.csproj --configuration Release --output ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'ai-voice-translator-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## Cấu hình Azure SQL Database

### 1. Tạo SQL Server (nếu chưa có)

Xem [AZURE_SETUP_GUIDE.md](./AZURE_SETUP_GUIDE.md) để tạo SQL Server và Database.

### 2. Cấu hình Firewall cho App Service

```bash
# Lấy outbound IPs của App Service
az webapp show \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --query outboundIpAddresses \
  --output tsv

# Thêm IP vào SQL Server Firewall
az sql server firewall-rule create \
  --resource-group rg-ai-voice-translator \
  --server ai-voice-translator-sql \
  --name AllowAppService \
  --start-ip-address <OUTBOUND_IP> \
  --end-ip-address <OUTBOUND_IP>
```

**Hoặc** cho phép Azure services:

```bash
az sql server firewall-rule create \
  --resource-group rg-ai-voice-translator \
  --server ai-voice-translator-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 3. Connection String Format

Connection string sẽ có dạng:
```
Server=tcp:ai-voice-translator-sql.database.windows.net,1433;Initial Catalog=AIVoiceTranslatorDB;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## Cấu hình Azure Redis Cache

### 1. Tạo Redis Cache

```bash
az redis create \
  --name redis-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --location southeastasia \
  --sku Basic \
  --vm-size c0
```

**Lưu ý**: 
- **Basic C0**: 250 MB, $15/tháng (cho development)
- **Standard C1**: 1 GB, $55/tháng (cho production)

### 2. Lấy Connection String

```bash
az redis list-keys \
  --name redis-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --query primaryConnectionString \
  --output tsv
```

Kết quả sẽ có dạng:
```
<name>.redis.cache.windows.net:6380,password=<password>,ssl=True,abortConnect=False
```

### 3. Cấu hình Firewall (nếu cần)

```bash
# Cho phép App Service access Redis
az redis firewall-rule create \
  --name AllowAppService \
  --redis-name redis-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --start-ip <APP_SERVICE_IP> \
  --end-ip <APP_SERVICE_IP>
```

---

## Cấu hình Application Settings

### 1. Thêm Connection Strings

Trong Azure Portal:
1. Vào **App Service** → **Configuration** → **Connection strings**
2. Thêm các connection strings:

| Name | Value | Type |
|------|-------|------|
| `DefaultConnection` | SQL connection string | SQLAzure |
| `Redis` | Redis connection string | Custom |

**Hoặc dùng Azure CLI:**

```bash
# SQL Connection String
az webapp config connection-string set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:...;Database=...;User ID=...;Password=...;"

# Redis Connection String
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings "ConnectionStrings:Redis=<redis-connection-string>"
```

### 2. Thêm Application Settings

```bash
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings \
    "StorageType=Azure" \
    "Azure:SpeechKey=<your-speech-key>" \
    "Azure:SpeechRegion=southeastasia" \
    "Azure:BlobConnectionString=<blob-connection-string>" \
    "Azure:AudioContainer=audio-files" \
    "Azure:OutputContainer=output-files" \
    "AzureTranslator:Endpoint=https://ai-translator-api.cognitiveservices.azure.com/" \
    "AzureTranslator:Key=<translator-key>" \
    "AzureTranslator:Region=southeastasia" \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"
```

### 3. Cấu hình OAuth (nếu dùng)

```bash
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings \
    "OAuth:Google:ClientId=<google-client-id>" \
    "OAuth:Google:ClientSecret=<google-client-secret>" \
    "OAuth:Facebook:AppId=<facebook-app-id>" \
    "OAuth:Facebook:AppSecret=<facebook-app-secret>" \
    "OAuth:Twitter:ConsumerKey=<twitter-consumer-key>" \
    "OAuth:Twitter:ConsumerSecret=<twitter-consumer-secret>"
```

### 4. Cấu hình MoMo Payment

```bash
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings \
    "MoMoPayment:MomoApiUrl=https://payment.momo.vn/gw_payment/transactionProcessor" \
    "MoMoPayment:SecretKey=<momo-secret-key>" \
    "MoMoPayment:AccessKey=<momo-access-key>" \
    "MoMoPayment:PartnerCode=<momo-partner-code>" \
    "MoMoPayment:RequestType=captureMoMoWallet" \
    "MoMoPayment:ReturnUrl=https://your-domain.com/Billing/PaymentReturn" \
    "MoMoPayment:NotifyUrl=https://your-domain.com/Billing/PaymentNotify"
```

**Lưu ý**: Thay `your-domain.com` bằng domain thực tế của bạn.

---

## Deploy Database Migrations

### Option A: Chạy Migration từ Local

```bash
cd src
dotnet ef database update --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
```

### Option B: Chạy Migration từ App Service (Kudu Console)

1. Vào App Service → **Advanced Tools** → **Go** → **Kudu**
2. Vào **Debug console** → **CMD**
3. Navigate đến thư mục site: `cd site/wwwroot`
4. Chạy migration:

```bash
dotnet ef database update
```

### Option C: Tạo Migration Script và chạy trên Azure SQL

```bash
# Tạo migration script
cd src
dotnet ef migrations script -o migration.sql

# Upload và chạy script trên Azure SQL Database
# (dùng Azure Portal hoặc SQL Server Management Studio)
```

---

## Cấu hình Azure Services

### 1. Azure Speech Service

Đã có trong [AZURE_SETUP_GUIDE.md](./AZURE_SETUP_GUIDE.md). Đảm bảo:
- Speech Key và Region đã được thêm vào App Settings
- Service đang chạy và có quota đủ

### 2. Azure Translator

Đã có trong [AZURE_SETUP_GUIDE.md](./AZURE_SETUP_GUIDE.md). Đảm bảo:
- Translator Key, Endpoint, và Region đã được thêm vào App Settings
- Endpoint URL đúng format: `https://<resource-name>.cognitiveservices.azure.com/`

### 3. Azure Blob Storage

Đã có trong [AZURE_SETUP_GUIDE.md](./AZURE_SETUP_GUIDE.md). Đảm bảo:
- Blob Connection String đã được thêm vào App Settings
- Containers `audio-files` và `output-files` đã được tạo
- CORS được cấu hình nếu cần (cho web uploads)

---

## Cấu hình Custom Domain & SSL

### 1. Thêm Custom Domain

```bash
az webapp config hostname add \
  --webapp-name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --hostname your-domain.com
```

### 2. Cấu hình DNS

Thêm CNAME record trỏ đến:
```
<app-name>.azurewebsites.net
```

### 3. Enable SSL (HTTPS)

Azure App Service tự động cung cấp SSL certificate cho `*.azurewebsites.net`.

Để dùng SSL cho custom domain:

**Option A: App Service Managed Certificate (Free)**

```bash
az webapp config ssl bind \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --certificate-name <certificate-name> \
  --ssl-type SNI
```

**Option B: Import Certificate**

1. Mua SSL certificate từ CA
2. Import vào App Service → **TLS/SSL settings** → **Private Key Certificates**
3. Bind certificate với custom domain

### 4. Force HTTPS Redirect

Trong `Program.cs`, đã có `app.UseHttpsRedirection()`, nhưng cần cấu hình thêm:

```bash
az webapp update \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --https-only true
```

---

## Cấu hình OAuth Providers

### 1. Google OAuth

1. Vào [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo OAuth 2.0 Client ID
3. Thêm Authorized redirect URIs:
   - `https://your-domain.com/signin-google`
   - `https://ai-voice-translator-app.azurewebsites.net/signin-google`
4. Copy Client ID và Client Secret vào App Settings

### 2. Facebook OAuth

1. Vào [Facebook Developers](https://developers.facebook.com/)
2. Tạo App → Add Facebook Login
3. Thêm Valid OAuth Redirect URIs:
   - `https://your-domain.com/signin-facebook`
4. Copy App ID và App Secret vào App Settings

### 3. Twitter OAuth

1. Vào [Twitter Developer Portal](https://developer.twitter.com/)
2. Tạo App
3. Thêm Callback URL:
   - `https://your-domain.com/signin-twitter`
4. Copy Consumer Key và Consumer Secret vào App Settings

---

## Cấu hình MoMo Payment

### 1. Cập nhật URLs trong MoMo Settings

Đảm bảo `ReturnUrl` và `NotifyUrl` trỏ đến production domain:

```bash
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings \
    "MoMoPayment:ReturnUrl=https://your-domain.com/Billing/PaymentReturn" \
    "MoMoPayment:NotifyUrl=https://your-domain.com/Billing/PaymentNotify"
```

### 2. Cấu hình MoMo Business Account

1. Đăng ký MoMo Business Account
2. Lấy Partner Code, Access Key, Secret Key
3. Cập nhật vào App Settings
4. Chuyển từ test environment sang production:
   - Test: `https://test-payment.momo.vn/...`
   - Production: `https://payment.momo.vn/...`

---

## Monitoring & Troubleshooting

### 1. Application Insights (Recommended)

```bash
# Tạo Application Insights
az monitor app-insights component create \
  --app ai-voice-translator-insights \
  --location southeastasia \
  --resource-group rg-ai-voice-translator

# Lấy Instrumentation Key
az monitor app-insights component show \
  --app ai-voice-translator-insights \
  --resource-group rg-ai-voice-translator \
  --query instrumentationKey \
  --output tsv

# Thêm vào App Settings
az webapp config appsettings set \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=<instrumentation-key>"
```

### 2. View Logs

**Stream Logs (Real-time):**
```bash
az webapp log tail \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator
```

**Download Logs:**
```bash
az webapp log download \
  --name ai-voice-translator-app \
  --resource-group rg-ai-voice-translator \
  --log-file logs.zip
```

**Trong Azure Portal:**
- App Service → **Log stream** (real-time)
- App Service → **Logs** → **Application Logging** (file logs)

### 3. Health Checks

Ứng dụng đã có health check endpoint tại `/health`. Cấu hình trong App Service:

1. App Service → **Health check**
2. Enable health check
3. Path: `/health`
4. Save

### 4. Common Issues

#### Issue: Database Connection Failed

**Giải pháp:**
- Kiểm tra SQL Server Firewall Rules
- Kiểm tra Connection String format
- Kiểm tra username/password

#### Issue: Redis Connection Failed

**Giải pháp:**
- Kiểm tra Redis Firewall Rules
- Kiểm tra Connection String (có `ssl=True`)
- Kiểm tra Redis service status

#### Issue: Hangfire Jobs Not Running

**Giải pháp:**
- Enable **Always On** trong App Service
- Kiểm tra Hangfire Dashboard: `https://your-domain.com/hangfire`
- Kiểm tra SQL connection (Hangfire dùng SQL storage)

#### Issue: Azure Services 401/403 Errors

**Giải pháp:**
- Kiểm tra Keys trong App Settings
- Kiểm tra Region settings
- Kiểm tra service quotas

#### Issue: Blob Storage Upload Failed

**Giải pháp:**
- Kiểm tra Blob Connection String
- Kiểm tra container names
- Kiểm tra container permissions

### 5. Performance Optimization

#### Enable Response Compression

Đã có trong `Program.cs`. Đảm bảo nó được enable.

#### Enable Caching

Redis caching đã được cấu hình. Monitor cache hit rate.

#### Database Indexing

Đã có indexes trong migrations. Có thể thêm indexes nếu cần.

#### CDN (Optional)

Có thể dùng Azure CDN cho static files:
```bash
az cdn profile create \
  --name cdn-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --sku Standard_Microsoft

az cdn endpoint create \
  --name cdn-endpoint-ai-voice-translator \
  --profile-name cdn-ai-voice-translator \
  --resource-group rg-ai-voice-translator \
  --origin "ai-voice-translator-app.azurewebsites.net"
```

---

## Checklist Deploy

- [ ] Azure App Service đã được tạo và deploy code
- [ ] Always On đã được enable
- [ ] Azure SQL Database đã được tạo và firewall rules đã cấu hình
- [ ] Database migrations đã chạy
- [ ] Azure Redis Cache đã được tạo và firewall rules đã cấu hình
- [ ] Tất cả Connection Strings đã được thêm vào App Settings
- [ ] Azure Speech Service keys đã được thêm
- [ ] Azure Translator keys đã được thêm
- [ ] Azure Blob Storage connection string đã được thêm
- [ ] Containers đã được tạo trong Blob Storage
- [ ] OAuth providers đã được cấu hình (nếu dùng)
- [ ] MoMo Payment URLs đã được cập nhật cho production
- [ ] Custom domain đã được thêm và SSL đã được cấu hình
- [ ] HTTPS redirect đã được enable
- [ ] Application Insights đã được cấu hình (optional nhưng recommended)
- [ ] Health checks đã được enable
- [ ] Logs đã được kiểm tra và không có errors
- [ ] Hangfire Dashboard có thể truy cập và jobs đang chạy
- [ ] Test upload audio file và verify end-to-end flow

---

## Cost Estimation (Monthly)

| Service | Tier | Cost (USD) |
|---------|------|------------|
| App Service | Basic B1 | ~$13 |
| SQL Database | Basic | ~$5 |
| Redis Cache | Basic C0 | ~$15 |
| Speech Service | Standard S0 | Pay-as-you-go |
| Translator | Standard S1 | Pay-as-you-go |
| Blob Storage | Standard LRS | ~$0.02/GB |
| **Total (Base)** | | **~$33 + usage** |

**Lưu ý**: Costs có thể thay đổi theo region và usage. Monitor costs trong Azure Cost Management.

---

## Tài liệu tham khảo

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/azure/azure-sql/database/)
- [Azure Redis Cache Documentation](https://docs.microsoft.com/azure/azure-cache-for-redis/)
- [ASP.NET Core Deployment](https://docs.microsoft.com/aspnet/core/host-and-deploy/)
- [Hangfire on Azure](https://docs.hangfire.io/en/latest/deployment-to-production/making-asp-net-app-always-running.html)

---

## Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Azure Portal → App Service → **Diagnose and solve problems**
2. Application Insights logs
3. Kudu Console logs
4. Azure Service Health





