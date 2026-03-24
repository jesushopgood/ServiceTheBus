$ErrorActionPreference = "Stop"

$resourceGroup = "dualservice-rg"
$acrName = "dualserviceacr"
$acrLoginServer = "$acrName.azurecr.io"

# Use a unique tag every run so Container Apps gets a template change and creates a new revision.
$releaseTag = (Get-Date).ToString("yyyyMMdd-HHmmss")

$leftLocalImage = "dualservice-serviceleft:latest"
$rightLocalImage = "dualservice-serviceright:latest"

$leftAcrImageRelease = "$acrLoginServer/dualservice-serviceleft:$releaseTag"
$rightAcrImageRelease = "$acrLoginServer/dualservice-serviceright:$releaseTag"

Write-Host "Building solution and Docker images..." -ForegroundColor Cyan
dotnet clean
dotnet build
docker compose build

Write-Host "Logging into ACR..." -ForegroundColor Cyan
az acr login --name $acrName

Write-Host "Tagging images with release tag $releaseTag..." -ForegroundColor Cyan
docker tag $leftLocalImage $leftAcrImageRelease
docker tag $rightLocalImage $rightAcrImageRelease

Write-Host "Pushing images to ACR..." -ForegroundColor Cyan
docker push $leftAcrImageRelease
docker push $rightAcrImageRelease

Write-Host "Updating Container Apps to immutable release images..." -ForegroundColor Cyan
az containerapp update --name service-left --resource-group $resourceGroup --image $leftAcrImageRelease
az containerapp update --name service-right --resource-group $resourceGroup --image $rightAcrImageRelease

Write-Host "Latest revisions after update:" -ForegroundColor Green
az containerapp show --name service-left --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv
az containerapp show --name service-right --resource-group $resourceGroup --query "properties.latestRevisionName" -o tsv

Write-Host "Current images configured on each app:" -ForegroundColor Green
az containerapp show --name service-left --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv
az containerapp show --name service-right --resource-group $resourceGroup --query "properties.template.containers[0].image" -o tsv

Write-Host "Recent tags in ACR:" -ForegroundColor Green
az acr repository show-tags --name $acrName --repository dualservice-serviceleft --orderby time_desc --top 5 -o table
az acr repository show-tags --name $acrName --repository dualservice-serviceright --orderby time_desc --top 5 -o table
