# AI Voice Translator SaaS

Cloud-ready ASP.NET Core MVC app that turns uploaded audio into translated speech using Azure services. Includes accurate duration detection with NAudio, caching, audit logging, and admin dashboards.

## Features
- Upload audio (MP3/WAV/M4A) and process asynchronously.
- Speech-to-Text with Azure Speech (continuous recognition for full-length audio).
- Translation with Azure Translator Text API (chunked for long texts).
- Text-to-Speech with Azure Speech (TTS) to generate downloadable audio.
- Accurate duration via NAudio (MediaFoundationReader for M4A on Windows).
- Redis caching for translation results.
- Audit logging for Register, Login, Upload, Download, Delete; admin log viewer with stats.
- Time display adjusted to UTC+7 in views.
- Responsive UI with improved spacing; auto-refresh every 10s only where needed.

## Tech Stack
- .NET 8, ASP.NET Core MVC, Entity Framework Core (SQL Server)
- Azure Speech (STT/TTS), Azure Translator, Azure Blob Storage
- Redis (caching), NAudio (duration)
- jQuery, Bootstrap, Toastr

## Prerequisites
- .NET 8 SDK
- SQL Server instance
- Redis instance (optional but recommended for caching)
- Azure resources: Speech, Translator, Storage (Blob)

## Configuration
Update `src/appsettings.json` (or `appsettings.Development.json`) with your values:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=AI_Translator;Trusted_Connection=True;TrustServerCertificate=True"
},
"AzureTranslator": {
  "Endpoint": "https://<your-translator>.cognitiveservices.azure.com",
  "Key": "<translator-key>",
  "Region": "<translator-region>"
},
"AzureSpeech": {
  "Key": "<speech-key>",
  "Region": "<speech-region>"
},
"AzureBlobStorage": {
  "ConnectionString": "<blob-connection-string>",
  "ContainerName": "<container>"
},
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

## Run Locally
```bash
cd src
dotnet restore
dotnet build
dotnet run
```
Then open `https://localhost:5001` (or the URL shown in console).

## Database
- EF Core migrations are included under `src/Migrations`.
- To initialize the database (if needed): `dotnet ef database update` (from `src`).

## Key Implementation Notes
- Translation is chunked in `Jobs/ProcessAudioJob.cs` to avoid timeouts.
- Duration is computed in `Services/AudioDurationService.cs` using NAudio; M4A uses `MediaFoundationReader` (Windows). Falls back to estimation if parsing fails.
- UI auto-refresh runs every 10s only when items are processing; loading overlay is suppressed for background polling.
- Audit logging is centralized via `AuditService`; view logs at `/Admin/Logs`.

## Troubleshooting
- M4A duration: ensure Windows Media Foundation components are available (on Windows Server, install Desktop Experience/Media Foundation features).
- Translator 404/401: verify the Translator endpoint includes `/translator/text/v3.0` if using the global endpoint; ensure key/region match your resource.
- Long audio not fully transcribed: continuous STT is enabled; if issues persist, check Azure Speech quotas and audio quality.

## Paths of Interest
- `Program.cs` – DI registrations for services (Translator, Speech, Storage, Caching, Audit, Duration).
- `Services/AudioDurationService.cs` – NAudio-based duration calculation.
- `Jobs/ProcessAudioJob.cs` – chunked translation pipeline.
- `Views/Admin/Logs.cshtml` – audit log viewer and stats.
- `wwwroot/css/site.css` – UI spacing and styling.

## Scripts & Pages
- Upload: `/Audio/Upload`
- Processing status: `/Audio/Processing/{id}`
- User dashboard: `/Dashboard`
- Admin dashboard: `/Admin/Dashboard`
- Admin logs: `/Admin/Logs`

## Licensing
Internal project; add license info here if needed.