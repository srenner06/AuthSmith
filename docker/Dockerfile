FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for better layer caching
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["AuthSmith.sln", "./"]

# Copy project files
COPY ["src/AuthSmith.Api/AuthSmith.Api.csproj", "src/AuthSmith.Api/"]
COPY ["src/AuthSmith.Application/AuthSmith.Application.csproj", "src/AuthSmith.Application/"]
COPY ["src/AuthSmith.Domain/AuthSmith.Domain.csproj", "src/AuthSmith.Domain/"]
COPY ["src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj", "src/AuthSmith.Infrastructure/"]
COPY ["src/AuthSmith.Contracts/AuthSmith.Contracts.csproj", "src/AuthSmith.Contracts/"]
COPY ["src/AuthSmith.Sdk/AuthSmith.Sdk.csproj", "src/AuthSmith.Sdk/"]

# Restore dependencies
RUN dotnet restore "AuthSmith.sln"

# Copy source code
COPY src/ ./src/

# Build
WORKDIR "/src/src/AuthSmith.Api"
RUN dotnet build "AuthSmith.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AuthSmith.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

USER appuser

ENTRYPOINT ["dotnet", "AuthSmith.Api.dll"]

