# Hướng dẫn Setup Azure Services cho AI Voice Translator SaaS

Hệ thống sử dụng các Azure Cognitive Services, Azure Storage, và Azure SQL Database để xử lý audio và dịch thuật. Tài liệu này hướng dẫn chi tiết cách thiết lập từng service.

## Mục lục

0. [Azure Resource Group](#0-azure-resource-group)
1. [Azure Speech Service (Speech-to-Text & Text-to-Speech)](#1-azure-speech-service)
2. [Azure Translator Text API](#2-azure-translator-text-api)
3. [Azure Blob Storage](#3-azure-blob-storage)
4. [Azure SQL Database](#4-azure-sql-database)
5. [Cấu hình trong appsettings.json](#5-cấu-hình-trong-appsettingsjson)
6. [Kiểm tra và Troubleshooting](#6-kiểm-tra-và-troubleshooting)

---

## 0. Azure Resource Group

Resource Group là container logic để tổ chức và quản lý các Azure resources. Nên tạo một Resource Group riêng cho project này để dễ quản lý.

### 0.1. Tạo Resource Group

1. **Đăng nhập Azure Portal**
   - Truy cập: https://portal.azure.com
   - Đăng nhập bằng tài khoản Microsoft Azure

2. **Tạo Resource Group**
   - Click **"Create a resource"** hoặc tìm kiếm **"Resource group"**
   - Chọn **"Resource group"**
   - Click **"Create"**

3. **Điền thông tin**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group name**: Đặt tên (ví dụ: `rg-ai-voice-translator`)
   - **Region**: Chọn region (khuyến nghị: `Southeast Asia` cho Việt Nam)
   - Click **"Review + create"** → **"Create"**

### 0.2. Quản lý Resource Group

**Lợi ích của Resource Group**:
- Tổ chức tất cả resources của project vào một nhóm
- Dễ dàng xóa toàn bộ resources khi không cần thiết
- Quản lý permissions và access control tập trung
- Theo dõi costs và usage theo Resource Group

**Best Practices**:
- Đặt tên Resource Group theo convention: `rg-{project-name}` hoặc `rg-{environment}-{project-name}`
- Sử dụng Tags để phân loại resources (ví dụ: Environment=Production, Project=VoiceTranslator)
- Enable Resource Group locks để tránh xóa nhầm trong production

**Lưu ý**: Khi tạo các resources khác (Speech, Translator, Storage, SQL), chọn Resource Group này để tổ chức tốt hơn.

---

## 1. Azure Speech Service

Azure Speech Service cung cấp 2 tính năng chính:
- **Speech-to-Text (STT)**: Chuyển đổi giọng nói thành văn bản
- **Text-to-Speech (TTS)**: Chuyển đổi văn bản thành giọng nói

### 1.1. Tạo Azure Speech Resource

1. **Đăng nhập Azure Portal**
   - Truy cập: https://portal.azure.com
   - Đăng nhập bằng tài khoản Microsoft Azure

2. **Tạo Speech Resource**
   - Click **"Create a resource"** hoặc **"+ Create"**
   - Tìm kiếm **"Speech"** hoặc **"Cognitive Services"**
   - Chọn **"Speech Services"** hoặc **"Speech"**
   - Click **"Create"**

3. **Điền thông tin**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group**: Tạo mới hoặc chọn resource group có sẵn
   - **Region**: Chọn region gần bạn (khuyến nghị: `Southeast Asia` cho Việt Nam)
   - **Name**: Đặt tên cho resource (ví dụ: `ai-voice-translator-speech`)
   - **Pricing tier**: 
     - **Free (F0)**: 5,000 transactions/tháng, phù hợp cho testing
     - **Standard (S0)**: Pay-as-you-go, phù hợp cho production
   - Click **"Review + create"** → **"Create"**

4. **Lấy thông tin API**
   - Sau khi tạo xong, vào resource vừa tạo
   - Vào **"Keys and Endpoint"** ở menu bên trái
   - Copy **KEY 1** (hoặc KEY 2) - đây là `SpeechKey`
   - Copy **Location/Region** - đây là `SpeechRegion` (ví dụ: `southeastasia`)

### 1.2. Các tính năng được sử dụng

- **Speech Recognition**: Nhận dạng giọng nói từ audio file (MP3, WAV, M4A)
- **Speech Synthesis**: Tạo giọng nói từ văn bản đã dịch
- **Auto Language Detection**: Tự động phát hiện ngôn ngữ (vi-VN, en-US, ja-JP)

### 1.3. Giới hạn và Pricing

**Free Tier (F0)**:
- 5,000 transactions/tháng
- Giới hạn 5 requests/phút
- Phù hợp cho development và testing

**Standard Tier (S0)**:
- Pay-as-you-go pricing
- $1.00 per 1,000 transactions (Speech-to-Text)
- $15.00 per 1 million characters (Text-to-Speech)
- Không giới hạn requests/phút

**Lưu ý**: 
- Mỗi audio file upload = 1 transaction (Speech-to-Text)
- Mỗi đoạn text dịch = tính theo số ký tự (Text-to-Speech)

---

## 2. Azure Translator Text API

Azure Translator Text API dịch văn bản giữa các ngôn ngữ.

### 2.1. Tạo Azure Translator Resource

1. **Tạo Translator Resource**
   - Trong Azure Portal, click **"Create a resource"**
   - Tìm kiếm **"Translator"**
   - Chọn **"Translator"** (Text Translation)
   - Click **"Create"**

2. **Điền thông tin**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group**: Tạo mới hoặc chọn resource group có sẵn
   - **Region**: Chọn region (khuyến nghị: `Southeast Asia`)
   - **Name**: Đặt tên (ví dụ: `ai-translator-api`)
   - **Pricing tier**:
     - **Free (F0)**: 2 triệu ký tự/tháng
     - **Standard (S1)**: Pay-as-you-go, $10 per 1 million characters
   - Click **"Review + create"** → **"Create"**

3. **Lấy thông tin API**
   - Vào resource vừa tạo
   - Vào **"Keys and Endpoint"**
   - Copy **KEY 1** - đây là `Key`
   - Copy **Endpoint** - đây là `Endpoint` (ví dụ: `https://ai-translator-api.cognitiveservices.azure.com/`)
   - Copy **Location/Region** - đây là `Region` (ví dụ: `southeastasia`)

### 2.2. Các tính năng được sử dụng

- **Text Translation**: Dịch văn bản giữa các ngôn ngữ
- **Language Detection**: Tự động phát hiện ngôn ngữ nguồn
- Hỗ trợ hơn 100 ngôn ngữ

### 2.3. Giới hạn và Pricing

**Free Tier (F0)**:
- 2 triệu ký tự/tháng
- Giới hạn 5 requests/phút
- Phù hợp cho development

**Standard Tier (S1)**:
- Pay-as-you-go: $10 per 1 million characters
- Không giới hạn requests/phút
- Phù hợp cho production

**Lưu ý**: 
- Hệ thống sử dụng caching để giảm số lần gọi API
- Mỗi đoạn text dịch được cache trong 24 giờ

---

## 3. Azure Blob Storage

Azure Blob Storage lưu trữ audio files và output files.

### 3.1. Tạo Storage Account

1. **Tạo Storage Account**
   - Trong Azure Portal, click **"Create a resource"**
   - Tìm kiếm **"Storage account"**
   - Chọn **"Storage account"**
   - Click **"Create"**

2. **Điền thông tin (Basics tab)**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group**: Tạo mới hoặc chọn resource group có sẵn
   - **Storage account name**: Đặt tên (phải unique globally, ví dụ: `aivoicetranslatorstorage`)
   - **Region**: Chọn region (khuyến nghị: `Southeast Asia`)
   - **Performance**: **Standard** (đủ cho hầu hết use cases)
   - **Redundancy**: **LRS** (Locally Redundant Storage) - đủ cho development, hoặc **GRS** (Geo-Redundant Storage) cho production
   - Click **"Review"** → **"Create"**

3. **Lấy Connection String**
   - Vào Storage Account vừa tạo
   - Vào **"Access keys"** ở menu bên trái
   - Copy **Connection string** của **key1** - đây là `BlobConnectionString`
   - Format: `DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net`

### 3.2. Tạo Containers

1. **Tạo Container cho Audio Files**
   - Vào **"Containers"** trong Storage Account
   - Click **"+ Container"**
   - **Name**: `audio-files` (hoặc tên bạn muốn)
   - **Public access level**: **Private** (không public)
   - Click **"Create"**

2. **Tạo Container cho Output Files**
   - Click **"+ Container"** lần nữa
   - **Name**: `output-files` (hoặc tên bạn muốn)
   - **Public access level**: **Private**
   - Click **"Create"**

### 3.3. Cấu hình CORS (nếu cần)

Nếu bạn cần truy cập files từ browser:

1. Vào **"Resource sharing (CORS)"** trong Storage Account
2. Cấu hình CORS cho Blob service:
   - **Allowed origins**: `*` (hoặc domain cụ thể)
   - **Allowed methods**: `GET, HEAD, OPTIONS`
   - **Allowed headers**: `*`
   - **Exposed headers**: `*`
   - **Max age**: `3600`

### 3.4. Giới hạn và Pricing

**Storage Account**:
- **LRS**: ~$0.018/GB/tháng
- **GRS**: ~$0.036/GB/tháng
- **Transaction costs**: $0.004 per 10,000 transactions

**Lưu ý**:
- Mỗi file upload/download = 1 transaction
- Nên sử dụng Lifecycle Management để tự động xóa files cũ sau một thời gian

---

## 4. Azure SQL Database

Azure SQL Database là managed database service để lưu trữ dữ liệu ứng dụng (users, audio files, transcripts, translations, audit logs).

### 4.1. Tạo SQL Server

1. **Tạo SQL Server**
   - Trong Azure Portal, click **"Create a resource"**
   - Tìm kiếm **"SQL server"** hoặc **"SQL Database"**
   - Chọn **"SQL Database"** → Click **"Create"**
   - Hoặc tạo **"SQL Server (logical server)"** trước, sau đó tạo Database

2. **Tạo SQL Server (logical server)**
   - Chọn **"SQL servers"** → Click **"Create"**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group**: Chọn Resource Group đã tạo (ví dụ: `rg-ai-voice-translator`)
   - **Server name**: Đặt tên (phải unique globally, ví dụ: `ai-voice-translator-sql`)
   - **Location**: Chọn region (khuyến nghị: `Southeast Asia`)
   - **Authentication method**: Chọn **"Use SQL authentication"**
   - **Server admin login**: Đặt username (ví dụ: `sqladmin`)
   - **Password**: Đặt password mạnh (lưu lại để dùng sau)
   - **Allow Azure services to access server**: **Yes** (nếu cần)
   - Click **"Review + create"** → **"Create"**

3. **Cấu hình Firewall Rules**
   - Sau khi tạo xong, vào SQL Server vừa tạo
   - Vào **"Networking"** ở menu bên trái
   - **Public network access**: **Selected networks** hoặc **Public access** (tùy nhu cầu)
   - **Firewall rules**:
     - Click **"+ Add your client IPv4 address"** để thêm IP của bạn
     - Hoặc thêm IP range: `0.0.0.0 - 255.255.255.255` (cho phép tất cả - chỉ dùng cho development)
   - Click **"Save"**

### 4.2. Tạo SQL Database

1. **Tạo Database**
   - Trong Azure Portal, click **"Create a resource"**
   - Tìm kiếm **"SQL Database"**
   - Chọn **"SQL Database"** → Click **"Create"**

2. **Điền thông tin (Basics tab)**
   - **Subscription**: Chọn subscription của bạn
   - **Resource group**: Chọn Resource Group đã tạo
   - **Database name**: Đặt tên (ví dụ: `AIVoiceTranslatorDB`)
   - **Server**: Chọn SQL Server vừa tạo (hoặc tạo mới)
   - **Want to use SQL elastic pool?**: **No** (cho development, có thể dùng Elastic Pool cho production)
   - **Compute + storage**: 
     - **Basic**: $5/tháng, 2GB storage, phù hợp cho development
     - **Standard S0**: ~$15/tháng, 250GB storage, phù hợp cho production nhỏ
     - **General Purpose**: Tùy chọn cho production lớn hơn
   - Click **"Review + create"** → **"Create"**

3. **Lấy Connection String**
   - Vào SQL Database vừa tạo
   - Vào **"Connection strings"** ở menu bên trái
   - Copy **ADO.NET** connection string
   - Thay `{your_password}` bằng password đã đặt khi tạo SQL Server
   - Format: `Server=tcp:{server-name}.database.windows.net,1433;Initial Catalog={database-name};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

### 4.3. Chạy Migrations

Sau khi có Database, chạy Entity Framework migrations để tạo tables:

```bash
cd src
dotnet ef database update
```

Hoặc nếu chưa có migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4.4. Cấu hình bảo mật

1. **Azure AD Authentication** (Production):
   - Cấu hình Azure AD để sử dụng Managed Identity
   - Không cần lưu password trong connection string

2. **Connection String trong Production**:
   - Sử dụng Azure Key Vault để lưu connection string
   - Hoặc sử dụng Environment Variables
   - KHÔNG commit connection string vào Git

3. **Backup và Restore**:
   - Azure SQL Database tự động backup mỗi ngày
   - Có thể restore về bất kỳ điểm nào trong 7-35 ngày (tùy tier)

### 4.5. Giới hạn và Pricing

**Basic Tier**:
- ~$5/tháng
- 2GB storage
- 5 DTU (Database Transaction Units)
- Phù hợp cho development và testing

**Standard S0**:
- ~$15/tháng
- 250GB storage
- 10 DTU
- Phù hợp cho production nhỏ

**General Purpose**:
- Từ ~$100/tháng trở lên
- Tùy chọn vCores và storage
- Phù hợp cho production lớn

**Lưu ý**:
- DTU là đơn vị đo performance (CPU, Memory, I/O)
- Có thể scale up/down tùy nhu cầu
- Storage có thể tăng đến 1TB hoặc hơn tùy tier

---

## 5. Cấu hình trong appsettings.json

Sau khi có đầy đủ thông tin từ các Azure services, cập nhật file `src/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:YOUR-SERVER-NAME.database.windows.net,1433;Initial Catalog=YOUR-DATABASE-NAME;Persist Security Info=False;User ID=YOUR-USERNAME;Password=YOUR-PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "Redis": "localhost:6379"
  },
  "StorageType": "Azure",
  "Azure": {
    "SpeechKey": "YOUR_SPEECH_KEY_HERE",
    "SpeechRegion": "southeastasia",
    "BlobConnectionString": "DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net",
    "AudioContainer": "audio-files",
    "OutputContainer": "output-files"
  },
  "AzureTranslator": {
    "Endpoint": "https://YOUR-RESOURCE-NAME.cognitiveservices.azure.com/",
    "Key": "YOUR_TRANSLATOR_KEY_HERE",
    "Region": "southeastasia"
  }
}
```

### Giải thích các tham số:

#### Azure Speech Service:
- **SpeechKey**: Key từ Azure Speech resource (Keys and Endpoint)
- **SpeechRegion**: Region của Speech resource (ví dụ: `southeastasia`, `eastasia`, `westus`)

#### Azure Translator:
- **Endpoint**: Endpoint URL từ Translator resource (Keys and Endpoint)
  - Format: `https://YOUR-RESOURCE-NAME.cognitiveservices.azure.com/`
  - Hoặc: `https://api.cognitive.microsofttranslator.com/` (global endpoint)
- **Key**: Key từ Translator resource
- **Region**: Region của Translator resource

#### Azure Blob Storage:
- **BlobConnectionString**: Connection string từ Storage Account (Access keys)
- **AudioContainer**: Tên container cho audio files (ví dụ: `audio-files`)
- **OutputContainer**: Tên container cho output files (ví dụ: `output-files`)

#### Azure SQL Database:
- **DefaultConnection**: Connection string từ SQL Database (Connection strings → ADO.NET)
  - Format: `Server=tcp:{server-name}.database.windows.net,1433;Initial Catalog={database-name};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
  - Thay `{server-name}`, `{database-name}`, `{username}`, `{password}` bằng giá trị thực tế

### Bảo mật (Production)

**KHÔNG** commit keys vào Git! Sử dụng một trong các cách sau:

#### Option 1: User Secrets (Development)
```bash
cd src
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_CONNECTION_STRING"
dotnet user-secrets set "Azure:SpeechKey" "YOUR_SPEECH_KEY"
dotnet user-secrets set "Azure:BlobConnectionString" "YOUR_BLOB_CONNECTION_STRING"
dotnet user-secrets set "AzureTranslator:Key" "YOUR_TRANSLATOR_KEY"
```

#### Option 2: Environment Variables
```bash
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection = "YOUR_SQL_CONNECTION_STRING"
$env:Azure__SpeechKey = "YOUR_SPEECH_KEY"
$env:Azure__BlobConnectionString = "YOUR_BLOB_CONNECTION_STRING"
$env:AzureTranslator__Key = "YOUR_TRANSLATOR_KEY"

# Linux/Mac
export ConnectionStrings__DefaultConnection="YOUR_SQL_CONNECTION_STRING"
export Azure__SpeechKey="YOUR_SPEECH_KEY"
export Azure__BlobConnectionString="YOUR_BLOB_CONNECTION_STRING"
export AzureTranslator__Key="YOUR_TRANSLATOR_KEY"
```

#### Option 3: Azure Key Vault (Production)
- Tạo Azure Key Vault
- Lưu các secrets vào Key Vault
- Cấu hình Managed Identity để app truy cập Key Vault
- Thêm Azure Key Vault configuration provider vào `Program.cs`

---

## 6. Kiểm tra và Troubleshooting

### 6.1. Kiểm tra Azure Speech Service

**Test Speech-to-Text**:
```csharp
// Code test trong C#
var config = SpeechConfig.FromSubscription("YOUR_SPEECH_KEY", "southeastasia");
var recognizer = new SpeechRecognizer(config);
var result = await recognizer.RecognizeOnceAsync();
Console.WriteLine($"Recognized: {result.Text}");
```

**Lỗi thường gặp**:
- **401 Unauthorized**: Kiểm tra lại SpeechKey và SpeechRegion
- **429 Too Many Requests**: Đã vượt quá giới hạn Free tier (5 requests/phút)
- **Audio format not supported**: Đảm bảo file là MP3, WAV, hoặc M4A

### 6.2. Kiểm tra Azure Translator

**Test Translation**:
```bash
# Sử dụng curl
curl -X POST "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=en&to=vi" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -H "Ocp-Apim-Subscription-Region: southeastasia" \
  -H "Content-Type: application/json" \
  -d "[{'Text':'Hello'}]"
```

**Lỗi thường gặp**:
- **401 Unauthorized**: Kiểm tra lại Key và Region
- **400 Bad Request**: Kiểm tra format của request body
- **404 Not Found**: Kiểm tra Endpoint URL có đúng không

### 6.3. Kiểm tra Azure Blob Storage

**Test Upload**:
```csharp
// Code test trong C#
var connectionString = "YOUR_CONNECTION_STRING";
var blobServiceClient = new BlobServiceClient(connectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("audio-files");
await containerClient.CreateIfNotExistsAsync();
```

**Lỗi thường gặp**:
- **403 Forbidden**: Kiểm tra Connection String và Access Key
- **404 Not Found**: Kiểm tra container name có đúng không
- **Container not found**: Tạo container trước khi upload

### 6.4. Kiểm tra Azure SQL Database

**Test Connection**:
```csharp
// Code test trong C#
using Microsoft.Data.SqlClient;

var connectionString = "YOUR_CONNECTION_STRING";
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
Console.WriteLine("Connected to Azure SQL Database!");
```

**Hoặc sử dụng SQL Server Management Studio (SSMS)**:
1. Download SSMS: https://docs.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms
2. Connect với thông tin:
   - **Server name**: `{server-name}.database.windows.net`
   - **Authentication**: SQL Server Authentication
   - **Login**: Username đã đặt khi tạo SQL Server
   - **Password**: Password đã đặt khi tạo SQL Server
3. Click **"Connect"**

**Lỗi thường gặp**:
- **Cannot open server**: Kiểm tra Firewall Rules đã thêm IP của bạn chưa
- **Login failed**: Kiểm tra username và password
- **Connection timeout**: Kiểm tra server name và network connection
- **Database not found**: Đảm bảo database đã được tạo

### 6.5. Kiểm tra tổng thể

1. **Kiểm tra logs**:
   - Xem logs trong Azure Portal (Application Insights nếu có)
   - Xem console logs khi chạy ứng dụng

2. **Kiểm tra quotas**:
   - Vào Azure Portal → Resource → Metrics
   - Xem số lượng requests đã sử dụng
   - Kiểm tra có vượt quá Free tier limit không

3. **Kiểm tra network**:
   - Đảm bảo server có thể kết nối internet
   - Kiểm tra firewall không chặn Azure endpoints

### 6.6. Cost Optimization Tips

1. **Sử dụng caching**:
   - Hệ thống đã có Redis cache cho translations
   - Giảm số lần gọi API Translator

2. **Sử dụng Lifecycle Management**:
   - Tự động xóa files cũ trong Blob Storage sau 30-90 ngày
   - Giảm chi phí storage

3. **Monitor usage**:
   - Set up alerts trong Azure Portal khi gần hết quota
   - Sử dụng Azure Cost Management để theo dõi chi phí

4. **Optimize requests**:
   - Batch multiple translations nếu có thể
   - Sử dụng async/await để xử lý song song

---

## 7. Tài liệu tham khảo

- **Azure Speech Service**: https://docs.microsoft.com/azure/cognitive-services/speech-service/
- **Azure Translator**: https://docs.microsoft.com/azure/cognitive-services/translator/
- **Azure Blob Storage**: https://docs.microsoft.com/azure/storage/blobs/
- **Azure SQL Database**: https://docs.microsoft.com/azure/azure-sql/database/
- **Azure Resource Groups**: https://docs.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal
- **Azure Pricing Calculator**: https://azure.microsoft.com/pricing/calculator/
- **Azure Free Account**: https://azure.microsoft.com/free/

---

## 8. Checklist Setup

- [ ] Tạo Azure Resource Group để tổ chức các resources
- [ ] Tạo Azure SQL Server và SQL Database
- [ ] Cấu hình Firewall Rules cho SQL Server
- [ ] Lấy SQL Connection String và cập nhật vào appsettings.json
- [ ] Chạy Entity Framework migrations để tạo database tables
- [ ] Tạo Azure Speech Resource và lấy SpeechKey + SpeechRegion
- [ ] Tạo Azure Translator Resource và lấy Key + Endpoint + Region
- [ ] Tạo Azure Storage Account và lấy ConnectionString
- [ ] Tạo 2 containers: `audio-files` và `output-files`
- [ ] Cập nhật `appsettings.json` với tất cả thông tin trên
- [ ] Test kết nối SQL Database (SSMS hoặc code)
- [ ] Test upload một audio file để kiểm tra Blob Storage
- [ ] Test transcription để kiểm tra Speech Service
- [ ] Test translation để kiểm tra Translator API
- [ ] Setup User Secrets hoặc Environment Variables cho production
- [ ] Monitor usage và costs trong Azure Portal
- [ ] Setup backup và disaster recovery cho SQL Database (production)

---

## 9. Support

Nếu gặp vấn đề:
- **Azure Support**: https://azure.microsoft.com/support/
- **Stack Overflow**: Tag `azure-cognitive-services`, `azure-speech`, `azure-translator`
- **GitHub Issues**: Tạo issue trong repository của project

**Lưu ý**: Luôn giữ bí mật các API keys và connection strings. Không chia sẻ chúng trong code, logs, hoặc public repositories.