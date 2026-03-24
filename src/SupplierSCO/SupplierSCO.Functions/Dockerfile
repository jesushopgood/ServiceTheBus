FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierSCO/SupplierSCO.Functions/SupplierSCO.Functions.csproj src/SupplierSCO/SupplierSCO.Functions/

# If the Functions project references other projects, copy their csproj files too:
# COPY src/SupplierSCO/SupplierSCO.Domain/SupplierSCO.Domain.csproj src/SupplierSCO/SupplierSCO.Domain/
# COPY src/SupplierSCO/SupplierSCO.Infrastructure/SupplierSCO.Infrastructure.csproj src/SupplierSCO/SupplierSCO.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierSCO/SupplierSCO.Functions/SupplierSCO.Functions.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierSCO/SupplierSCO.Functions/SupplierSCO.Functions.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
WORKDIR /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /app/publish .