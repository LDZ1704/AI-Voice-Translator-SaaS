# Hướng dẫn Setup Azure Services

## 1. Azure Resource Group

1. Đăng nhập Azure Portal: https://portal.azure.com
2. Tạo Resource Group:
   - Tên: `rg-ai-voice-translator`
   - Region: `Southeast Asia`
   - Click **Create**

**Lưu ý**: Chọn Resource Group này khi tạo các services khác.

## 2. Azure Speech Service

### Tạo Resource
1. Azure Portal → **Create a resource** → Tìm **"Speech"**
2. Điền thông tin:
   - **Resource group**: Chọn Resource Group đã tạo
   - **Region**: `Southeast Asia`
   - **Name**: `ai-voice-translator-speech`
   - **Pricing tier**: `Free (F0)` cho test, `Standard (S0)` cho production
3. Lấy thông tin:
   - Vào resource → **Keys and Endpoint**
   - Copy **KEY 1** → `SpeechKey`
   - Copy **Location/Region** → `SpeechRegion` (ví dụ: `southeastasia`)

### Giới hạn
- **Free (F0)**: 5,000 transactions/tháng, 5 requests/phút
- **Standard (S0)**: Pay-as-you-go, $1/1,000 transactions (STT), $15/1M characters (TTS)

## 3. Azure Translator Text API

### Tạo Resource
1. Azure Portal → **Create a resource** → Tìm **"Translator"**
2. Điền thông tin:
   - **Resource group**: Chọn Resource Group đã tạo
   - **Region**: `Southeast Asia`
   - **Name**: `ai-translator-api`
   - **Pricing tier**: `Free (F0)` cho test, `Standard (S1)` cho production
3. Lấy thông tin:
   - Vào resource → **Keys and Endpoint**
   - Copy **KEY 1** → `Key`
   - Copy **Endpoint** → `Endpoint`
   - Copy **Location/Region** → `Region`

### Giới hạn
- **Free (F0)**: 2 triệu ký tự/tháng, 5 requests/phút
- **Standard (S1)**: $10/1 triệu ký tự

## 4. Azure Blob Storage

### Tạo Storage Account
1. Azure Portal → **Create a resource** → Tìm **"Storage account"**
2. Điền thông tin:
   - **Resource group**: Chọn Resource Group đã tạo
   - **Storage account name**: Tên unique globally (ví dụ: `aivoicetranslatorstorage`)
   - **Region**: `Southeast Asia`
   - **Performance**: `Standard`
   - **Redundancy**: `LRS` (development) hoặc `GRS` (production)
3. Lấy Connection String:
   - Vào Storage Account → **Access keys**
   - Copy **Connection string** của **key1** → `BlobConnectionString`

### Tạo Containers
1. Vào **Containers** trong Storage Account
2. Tạo 2 containers:
   - `audio-files` (Private)
   - `output-files` (Private)

## 5. Azure SQL Database

### Tạo SQL Server
1. Azure Portal → **Create a resource** → Tìm **"SQL server"**
2. Điền thông tin:
   - **Resource group**: Chọn Resource Group đã tạo
   - **Server name**: Tên unique globally (ví dụ: `ai-voice-translator-sql`)
   - **Location**: `Southeast Asia`
   - **Authentication**: SQL authentication
   - **Server admin login**: Đặt username
   - **Password**: Đặt password mạnh
3. Cấu hình Firewall:
   - Vào SQL Server → **Networking**
   - Click **"+ Add your client IPv4 address"**
   - Hoặc thêm `0.0.0.0 - 255.255.255.255` (chỉ cho development)

### Tạo Database
1. Azure Portal → **Create a resource** → Tìm **"SQL Database"**
2. Điền thông tin:
   - **Resource group**: Chọn Resource Group đã tạo
   - **Database name**: `AIVoiceTranslatorDB`
   - **Server**: Chọn SQL Server vừa tạo
   - **Compute + storage**: `Basic` ($5/tháng) cho development, `Standard S0` ($15/tháng) cho production
3. Lấy Connection String:
   - Vào Database → **Connection strings** → Copy **ADO.NET**
   - Thay `{your_password}` bằng password đã đặt

### Chạy Migrations
```bash
cd src
dotnet ef database update
```

## 6. Cấu hình appsettings.json

Cập nhật `src/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:YOUR-SERVER.database.windows.net,1433;Initial Catalog=YOUR-DB;User ID=YOUR-USERNAME;Password=YOUR-PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "Redis": "localhost:6379"
  },
  "StorageType": "Azure",
  "Azure": {
    "SpeechKey": "YOUR_SPEECH_KEY",
    "SpeechRegion": "southeastasia",
    "BlobConnectionString": "DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net",
    "AudioContainer": "audio-files",
    "OutputContainer": "output-files"
  },
  "AzureTranslator": {
    "Endpoint": "https://YOUR-RESOURCE.cognitiveservices.azure.com/",
    "Key": "YOUR_TRANSLATOR_KEY",
    "Region": "southeastasia"
  }
}
```

## 7. Bảo mật (Production)

**KHÔNG commit keys vào Git!**

### User Secrets (Development)
```bash
cd src
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "Azure:SpeechKey" "YOUR_SPEECH_KEY"
dotnet user-secrets set "Azure:BlobConnectionString" "YOUR_BLOB_CONNECTION_STRING"
dotnet user-secrets set "AzureTranslator:Key" "YOUR_TRANSLATOR_KEY"
```

### Azure Key Vault (Production)
- Tạo Azure Key Vault
- Lưu secrets vào Key Vault
- Cấu hình Managed Identity
- Thêm Key Vault configuration provider vào `Program.cs`

## 8. Troubleshooting

### Speech Service
- **401 Unauthorized**: Kiểm tra `SpeechKey` và `SpeechRegion`
- **429 Too Many Requests**: Đã vượt Free tier limit (5 requests/phút)

### Translator
- **401 Unauthorized**: Kiểm tra `Key` và `Region`
- **404 Not Found**: Kiểm tra `Endpoint` URL

### Blob Storage
- **403 Forbidden**: Kiểm tra Connection String
- **404 Not Found**: Kiểm tra container name

### SQL Database
- **Cannot open server**: Kiểm tra Firewall Rules
- **Login failed**: Kiểm tra username/password
- **Connection timeout**: Kiểm tra server name và network

## Tài liệu tham khảo

- [Azure Speech Service](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Azure Translator](https://docs.microsoft.com/azure/cognitive-services/translator/)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/)
- [Azure SQL Database](https://docs.microsoft.com/azure/azure-sql/database/)