FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierWAL/SupplierWAL.Api/SupplierWAL.Api.csproj src/SupplierWAL/SupplierWAL.Api/

# If the API references other projects, copy their csproj files too:
# COPY src/SupplierWAL/SupplierWAL.Domain/SupplierWAL.Domain.csproj src/SupplierWAL/SupplierWAL.Domain/
# COPY src/SupplierWAL/SupplierWAL.Infrastructure/SupplierWAL.Infrastructure.csproj src/SupplierWAL/SupplierWAL.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierWAL/SupplierWAL.Api/SupplierWAL.Api.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierWAL/SupplierWAL.Api/SupplierWAL.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SupplierWAL.Api.dll"]