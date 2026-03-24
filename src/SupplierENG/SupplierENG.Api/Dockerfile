FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first
COPY src/SupplierENG/SupplierENG.Api/SupplierENG.Api.csproj src/SupplierENG/SupplierENG.Api/

# If the API references other projects, copy their csproj files too:
# COPY src/SupplierENG/SupplierENG.Domain/SupplierENG.Domain.csproj src/SupplierENG/SupplierENG.Domain/
# COPY src/SupplierENG/SupplierENG.Infrastructure/SupplierENG.Infrastructure.csproj src/SupplierENG/SupplierENG.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/SupplierENG/SupplierENG.Api/SupplierENG.Api.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/SupplierENG/SupplierENG.Api/SupplierENG.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SupplierENG.Api.dll"]