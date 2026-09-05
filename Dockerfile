# Use the official .NET SDK image for building and publishing
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG SERVICE_NAME

WORKDIR /src

# Copy the solution and restore dependencies
COPY *.sln .
COPY src/TradeBridge.Contracts/*.csproj src/TradeBridge.Contracts/
COPY src/TradeBridge.Shared/*.csproj src/TradeBridge.Shared/
COPY src/TradeBridge.ApiGateway/*.csproj src/TradeBridge.ApiGateway/
COPY src/TradeBridge.OrderService/*.csproj src/TradeBridge.OrderService/
COPY src/TradeBridge.RiskService/*.csproj src/TradeBridge.RiskService/
COPY src/TradeBridge.ExecutionService/*.csproj src/TradeBridge.ExecutionService/
COPY src/TradeBridge.LiquidityProvider/*.csproj src/TradeBridge.LiquidityProvider/
COPY tests/TradeBridge.UnitTests/*.csproj tests/TradeBridge.UnitTests/
COPY tests/TradeBridge.IntegrationTests/*.csproj tests/TradeBridge.IntegrationTests/

RUN dotnet restore

# Copy the remaining source code
COPY . .

# Build and publish the target service
WORKDIR /src/src/${SERVICE_NAME}
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
ARG SERVICE_NAME
WORKDIR /app

# Upgrade system packages to resolve base image vulnerabilities (e.g. OpenSSL)
RUN apk upgrade --no-cache

# Expose common port
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

# Use a shell script to run the variable entrypoint since Docker CMD does not expand ARGs
RUN echo "dotnet ${SERVICE_NAME}.dll" > /app/entrypoint.sh && chmod +x /app/entrypoint.sh
ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]
