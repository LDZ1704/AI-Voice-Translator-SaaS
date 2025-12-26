# PowerShell script để deploy ứng dụng lên Azure
# Usage: .\scripts\deploy.ps1

param(
    [string]$ResourceGroup = "rg-ai-voice-translator",
    [string]$AppName = "ai-voice-translator-app",
    [string]$Configuration = "Release"
)

Write-Host "Starting deployment to Azure..." -ForegroundColor Green

# Kiểm tra Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Azure CLI chưa được cài đặt. Vui lòng cài đặt từ https://aka.ms/installazurecliwindows" -ForegroundColor Red
    exit 1
}

# Kiểm tra đăng nhập Azure
Write-Host "Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Chưa đăng nhập Azure. Đang đăng nhập..." -ForegroundColor Yellow
    az login
}

# Build project
Write-Host "Building project..." -ForegroundColor Yellow
Set-Location src
dotnet restore
dotnet build --configuration $Configuration

# Publish project
Write-Host "Publishing project..." -ForegroundColor Yellow
$publishPath = ".\publish"
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}
dotnet publish --configuration $Configuration --output $publishPath

# Tạo zip file
Write-Host "Creating deployment package..." -ForegroundColor Yellow
$zipPath = "..\deploy.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force

# Deploy to Azure
Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $AppName `
    --src $zipPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "App URL: https://$AppName.azurewebsites.net" -ForegroundColor Cyan
} else {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

# Cleanup
Remove-Item $zipPath -Force
Set-Location ..

Write-Host "Done!" -ForegroundColor Green





