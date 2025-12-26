# PowerShell script để tạo tất cả Azure resources cần thiết
# Usage: .\scripts\setup-azure-resources.ps1

param(
    [string]$ResourceGroup = "rg-ai-voice-translator",
    [string]$Location = "southeastasia",
    [string]$SqlServerName = "ai-voice-translator-sql",
    [string]$SqlAdminUser = "sqladmin",
    [string]$SqlAdminPassword = "",
    [string]$DatabaseName = "AIVoiceTranslatorDB",
    [string]$RedisName = "redis-ai-voice-translator",
    [string]$StorageAccountName = "aivoicetranslatorstorage",
    [string]$AppServicePlanName = "plan-ai-voice-translator",
    [string]$AppName = "ai-voice-translator-app"
)

Write-Host "Setting up Azure resources..." -ForegroundColor Green

# Kiểm tra Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Azure CLI chưa được cài đặt." -ForegroundColor Red
    exit 1
}

# Đăng nhập nếu cần
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Đang đăng nhập Azure..." -ForegroundColor Yellow
    az login
}

# Tạo Resource Group
Write-Host "Creating Resource Group..." -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location

# Tạo App Service Plan
Write-Host "Creating App Service Plan..." -ForegroundColor Yellow
az appservice plan create `
    --name $AppServicePlanName `
    --resource-group $ResourceGroup `
    --sku B1 `
    --is-linux

# Tạo Web App
Write-Host "Creating Web App..." -ForegroundColor Yellow
az webapp create `
    --name $AppName `
    --resource-group $ResourceGroup `
    --plan $AppServicePlanName `
    --runtime "DOTNET|8.0"

# Enable Always On
Write-Host "Enabling Always On..." -ForegroundColor Yellow
az webapp config set `
    --name $AppName `
    --resource-group $ResourceGroup `
    --always-on true

# Tạo SQL Server (nếu chưa có password, sẽ yêu cầu nhập)
if ([string]::IsNullOrEmpty($SqlAdminPassword)) {
    $SqlAdminPassword = Read-Host "Nhập SQL Admin Password" -AsSecureString
    $SqlAdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
    )
}

Write-Host "Creating SQL Server..." -ForegroundColor Yellow
az sql server create `
    --name $SqlServerName `
    --resource-group $ResourceGroup `
    --location $Location `
    --admin-user $SqlAdminUser `
    --admin-password $SqlAdminPassword

# Cho phép Azure services access SQL
Write-Host "Configuring SQL Firewall..." -ForegroundColor Yellow
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Tạo SQL Database
Write-Host "Creating SQL Database..." -ForegroundColor Yellow
az sql db create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $DatabaseName `
    --service-objective Basic

# Tạo Redis Cache
Write-Host "Creating Redis Cache..." -ForegroundColor Yellow
az redis create `
    --name $RedisName `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Basic `
    --vm-size c0

# Tạo Storage Account
Write-Host "Creating Storage Account..." -ForegroundColor Yellow
az storage account create `
    --name $StorageAccountName `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Standard_LRS

# Tạo containers trong Storage Account
Write-Host "Creating Blob Containers..." -ForegroundColor Yellow
$storageKey = az storage account keys list `
    --resource-group $ResourceGroup `
    --account-name $StorageAccountName `
    --query "[0].value" `
    --output tsv

az storage container create `
    --name "audio-files" `
    --account-name $StorageAccountName `
    --account-key $storageKey `
    --public-access off

az storage container create `
    --name "output-files" `
    --account-name $StorageAccountName `
    --account-key $storageKey `
    --public-access off

Write-Host "Azure resources setup completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Tạo Azure Speech Service và Translator (xem AZURE_SETUP_GUIDE.md)"
Write-Host "2. Cấu hình App Settings trong Azure Portal"
Write-Host "3. Chạy database migrations: dotnet ef database update"
Write-Host "4. Deploy code: .\scripts\deploy.ps1"





