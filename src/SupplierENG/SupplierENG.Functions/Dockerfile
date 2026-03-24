FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierENG/SupplierENG.Functions/SupplierENG.Functions.csproj src/SupplierENG/SupplierENG.Functions/

# If the Functions project references other projects, copy their csproj files too:
# COPY src/SupplierENG/SupplierENG.Domain/SupplierENG.Domain.csproj src/SupplierENG/SupplierENG.Domain/
# COPY src/SupplierENG/SupplierENG.Infrastructure/SupplierENG.Infrastructure.csproj src/SupplierENG/SupplierENG.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierENG/SupplierENG.Functions/SupplierENG.Functions.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierENG/SupplierENG.Functions/SupplierENG.Functions.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
WORKDIR /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /app/publish .