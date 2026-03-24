FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierSCO/SupplierSCO.Api/SupplierSCO.Api.csproj src/SupplierSCO/SupplierSCO.Api/

# If the API references other projects, copy their csproj files too:
# COPY src/SupplierSCO/SupplierSCO.Domain/SupplierSCO.Domain.csproj src/SupplierSCO/SupplierSCO.Domain/
# COPY src/SupplierSCO/SupplierSCO.Infrastructure/SupplierSCO.Infrastructure.csproj src/SupplierSCO/SupplierSCO.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierSCO/SupplierSCO.Api/SupplierSCO.Api.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierSCO/SupplierSCO.Api/SupplierSCO.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SupplierSCO.Api.dll"]