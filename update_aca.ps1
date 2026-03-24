$ErrorActionPreference = "Stop"

$resourceGroup = "rg-servicethebus"
$acrName = "servicethebusacry9emi1"
$acrLoginServer = "$acrName.azurecr.io"

# Use a unique tag every run so Container Apps gets a template change and creates a new revision.
$releaseTag = (Get-Date).ToString("yyyyMMdd-HHmmss")

$orderServiceApiLocalImage = "orderservice-api:latest"
$orderServiceFuncLocalImage = "orderservice-func:latest"

$supplierEngFuncLocalImage = "suppliereng-func:latest"
$supplierScoFuncLocalImage = "suppliersco-func:latest"
$supplierWalFuncLocalImage = "supplierwal-func:latest"

$orderServiceApiAcrImageRelease = "$acrLoginServer/orderservice-api:$releaseTag"
$orderServiceFuncAcrImageRelease = "$acrLoginServer/orderservice-func:$releaseTag"

$supplierEngFuncAcrImageRelease = "$acrLoginServer/suppliereng-func:$releaseTag"
$supplierScoFuncAcrImageRelease = "$acrLoginServer/suppliersco-func:$releaseTag"
$supplierWalFuncAcrImageRelease = "$acrLoginServer/supplierwal-func:$releaseTag"

Write-Host "Applying Terraform changes for function host configuration..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot/terraform"
terraform apply
Pop-Location

Write-Host "Terraform applied with PostgreSQL provisioned and wired into Container App secrets." -ForegroundColor Green

Write-Host "Building solution and Docker images..." -ForegroundColor Cyan
dotnet clean
dotnet build
docker compose build orderservice-api orderservice-func
docker compose build suppliereng-func suppliersco-func supplierwal-func

Write-Host "Logging into ACR..." -ForegroundColor Cyan
az acr login --name $acrName

Write-Host "Tagging images with release tag $releaseTag..." -ForegroundColor Cyan
docker tag $orderServiceApiLocalImage $orderServiceApiAcrImageRelease
docker tag $orderServiceFuncLocalImage $orderServiceFuncAcrImageRelease

docker tag $supplierEngFuncLocalImage $supplierEngFuncAcrImageRelease

docker tag $supplierScoFuncLocalImage $supplierScoFuncAcrImageRelease

docker tag $supplierWalFuncLocalImage $supplierWalFuncAcrImageRelease

Write-Host "Pushing images to ACR..." -ForegroundColor Cyan
docker push $orderServiceApiAcrImageRelease
docker push $orderServiceFuncAcrImageRelease

docker push $supplierEngFuncAcrImageRelease

docker push $supplierScoFuncAcrImageRelease

docker push $supplierWalFuncAcrImageRelease

Write-Host "Updating Container Apps to immutable release images..." -ForegroundColor Cyan
az containerapp update --name orderservice-api --resource-group $resourceGroup --image $orderServiceApiAcrImageRelease
az containerapp update --name orderservice-func --resource-group $resourceGroup --image $orderServiceFuncAcrImageRelease

az containerapp update --name suppliereng-func --resource-group $resourceGroup --image $supplierEngFuncAcrImageRelease

az containerapp update --name suppliersco-func --resource-group $resourceGroup --image $supplierScoFuncAcrImageRelease

az containerapp update --name supplierwal-func --resource-group $resourceGroup --image $supplierWalFuncAcrImageRelease


Write-Host "Latest revisions after update:" -ForegroundColor Green
az containerapp show --name orderservice-api --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv
az containerapp show --name orderservice-func --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv

az containerapp show --name suppliereng-func --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv

az containerapp show --name suppliersco-func --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv

az containerapp show --name supplierwal-func --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv


Write-Host "Current images configured on each app:" -ForegroundColor Green
az containerapp show --name orderservice-api --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv
az containerapp show --name orderservice-func --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv

az containerapp show --name suppliereng-func --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv

az containerapp show --name suppliersco-func --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv

az containerapp show --name supplierwal-func --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv

Write-Host "Recent tags in ACR:" -ForegroundColor Green
az acr repository show-tags --name $acrName --repository orderservice-api --orderby time_desc --top 5 -o table
az acr repository show-tags --name $acrName --repository orderservice-func --orderby time_desc --top 5 -o table

az acr repository show-tags --name $acrName --repository suppliereng-func --orderby time_desc --top 5 -o table

az acr repository show-tags --name $acrName --repository suppliersco-func --orderby time_desc --top 5 -o table

az acr repository show-tags --name $acrName --repository supplierwal-func --orderby time_desc --top 5 -o table
