#!/bin/bash
# Bash script để deploy ứng dụng lên Azure
# Usage: ./scripts/deploy.sh

RESOURCE_GROUP="rg-ai-voice-translator"
APP_NAME="ai-voice-translator-app"
CONFIGURATION="Release"

echo "Starting deployment to Azure..."

# Kiểm tra Azure CLI
if ! command -v az &> /dev/null; then
    echo "Azure CLI chưa được cài đặt. Vui lòng cài đặt từ https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Kiểm tra đăng nhập Azure
echo "Checking Azure login..."
if ! az account show &> /dev/null; then
    echo "Chưa đăng nhập Azure. Đang đăng nhập..."
    az login
fi

# Build project
echo "Building project..."
cd src
dotnet restore
dotnet build --configuration $CONFIGURATION

# Publish project
echo "Publishing project..."
PUBLISH_PATH="./publish"
rm -rf $PUBLISH_PATH
dotnet publish --configuration $CONFIGURATION --output $PUBLISH_PATH

# Tạo zip file
echo "Creating deployment package..."
ZIP_PATH="../deploy.zip"
rm -f $ZIP_PATH
cd $PUBLISH_PATH
zip -r $ZIP_PATH .
cd ../..

# Deploy to Azure
echo "Deploying to Azure App Service..."
az webapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --src $ZIP_PATH

if [ $? -eq 0 ]; then
    echo "Deployment completed successfully!"
    echo "App URL: https://$APP_NAME.azurewebsites.net"
else
    echo "Deployment failed!"
    exit 1
fi

# Cleanup
rm -f $ZIP_PATH
cd ..

echo "Done!"





