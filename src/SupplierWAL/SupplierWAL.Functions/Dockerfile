FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierWAL/SupplierWAL.Functions/SupplierWAL.Functions.csproj src/SupplierWAL/SupplierWAL.Functions/

# If the Functions project references other projects, copy their csproj files too:
# COPY src/SupplierWAL/SupplierWAL.Domain/SupplierWAL.Domain.csproj src/SupplierWAL/SupplierWAL.Domain/
# COPY src/SupplierWAL/SupplierWAL.Infrastructure/SupplierWAL.Infrastructure.csproj src/SupplierWAL/SupplierWAL.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierWAL/SupplierWAL.Functions/SupplierWAL.Functions.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierWAL/SupplierWAL.Functions/SupplierWAL.Functions.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
WORKDIR /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /app/publish .