# AI Voice Translator SaaS

Cloud-ready ASP.NET Core MVC app that converts uploaded audio into translated speech using Azure services.

## Features

- **Audio Processing**: Upload MP3/WAV/M4A files, process asynchronously
- **Speech-to-Text**: Azure Speech Service with continuous recognition
- **Translation**: Azure Translator Text API with chunked processing
- **Text-to-Speech**: Azure Speech TTS to generate downloadable audio
- **Duration Detection**: Accurate duration via NAudio
- **Caching**: Redis caching for translation results
- **Audit Logging**: Track Register, Login, Upload, Download, Delete
- **Admin Dashboard**: User management, uploads, logs, statistics
- **Subscription Management**: Trial, Basic, Standard, Premium plans with MoMo payment
- **Responsive UI**: Bootstrap 5 with improved spacing and auto-refresh

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core MVC, Entity Framework Core (SQL Server)
- **Azure Services**: Speech (STT/TTS), Translator, Blob Storage, SQL Database
- **Caching**: Redis
- **Audio Processing**: NAudio
- **Payment**: MoMo Payment Gateway
- **Frontend**: jQuery, Bootstrap 5, Toastr

## Prerequisites

- .NET 8 SDK
- SQL Server (Azure SQL Database hoặc local)
- Redis (optional, recommended for caching)
- Azure resources: Speech, Translator, Blob Storage
- MoMo Business account (for payment)

## Quick Start

### 1. Clone và restore
```bash
git clone <repository-url>
cd AI-Voice-Translator-SaaS
cd src
dotnet restore
```

### 2. Cấu hình Azure
Xem [AZURE_SETUP_GUIDE.md](./AZURE_SETUP_GUIDE.md) để setup Azure services.

### 3. Cấu hình MoMo Payment (optional)
Xem [MOMO_SETUP_GUIDE.md](./MOMO_SETUP_GUIDE.md) để setup payment gateway.

### 4. Cấu hình appsettings.json
Cập nhật `src/appsettings.json` với Azure credentials và connection strings.

### 5. Database Migration
```bash
cd src
dotnet ef database update
```

### 6. Run
```bash
dotnet run
```
Mở `https://localhost:5001` (hoặc URL hiển thị trong console).

## Configuration

Cập nhật `src/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AI_Translator;Trusted_Connection=True;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "StorageType": "Azure",
  "Azure": {
    "SpeechKey": "<speech-key>",
    "SpeechRegion": "<speech-region>",
    "BlobConnectionString": "<blob-connection-string>",
    "AudioContainer": "audio-files",
    "OutputContainer": "output-files"
  },
  "AzureTranslator": {
    "Endpoint": "https://<translator>.cognitiveservices.azure.com/",
    "Key": "<translator-key>",
    "Region": "<translator-region>"
  },
  "MoMoPayment": {
    "MomoApiUrl": "https://test-payment.momo.vn/gw_payment/transactionProcessor",
    "SecretKey": "<secret-key>",
    "AccessKey": "<access-key>",
    "PartnerCode": "MOMO",
    "RequestType": "captureMoMoWallet"
  }
}
```

## Subscription Plans

- **Trial**: Unlimited time, 5 conversions
- **Basic**: 150,000₫/month, 500 conversions/month
- **Standard**: 500,000₫/month, 1000 conversions/month
- **Premium**: 1,000,000₫/month, 5000 conversions/month

## Key Files

- `Program.cs` - DI registrations
- `Services/AudioDurationService.cs` - NAudio duration calculation
- `Jobs/ProcessAudioJob.cs` - Chunked translation pipeline
- `Services/MoMoPaymentService.cs` - Payment integration
- `Services/SubscriptionService.cs` - Subscription management
- `Views/Admin/Logs.cshtml` - Audit log viewer

## Troubleshooting

- **M4A duration**: Install Windows Media Foundation components
- **Translator 404/401**: Verify endpoint includes `/translator/text/v3.0`, check key/region
- **Long audio not transcribed**: Check Azure Speech quotas and audio quality
- **MoMo payment errors**: Check signature encoding (UTF-8) and field order

## Documentation

- [Azure Setup Guide](./AZURE_SETUP_GUIDE.md) - Hướng dẫn setup Azure services
- [MoMo Payment Setup Guide](./MOMO_SETUP_GUIDE.md) - Hướng dẫn setup payment gateway
- [Deployment Guide](./DEPLOYMENT_GUIDE.md) - Hướng dẫn deploy lên Azure chi tiết

## License

Internal project.