FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy only the project files first
COPY src/OrderService/OrderService.Api/OrderService.Api.csproj src/OrderService/OrderService.Api/
# Repeat for other referenced projects if needed
# COPY src/OrderService/OrderService.Domain/OrderService.Domain.csproj src/OrderService/OrderService.Domain/
# COPY src/OrderService/OrderService.Infrastructure/OrderService.Infrastructure.csproj src/OrderService/OrderService.Infrastructure/

# Restore dependencies (cached unless csproj changes)
RUN dotnet restore src/OrderService/OrderService.Api/OrderService.Api.csproj

# Now copy the rest of the source
COPY . .

# Publish
RUN dotnet publish src/OrderService/OrderService.Api/OrderService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "OrderService.Api.dll"]