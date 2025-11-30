FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0
ARG BUILD_NUMBER=0
ARG COMMIT_HASH=unknown

WORKDIR /src

# Copy solution and project files
COPY ["AuthSmith.slnx", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/AuthSmith.Api/AuthSmith.Api.csproj", "src/AuthSmith.Api/"]
COPY ["src/AuthSmith.Application/AuthSmith.Application.csproj", "src/AuthSmith.Application/"]
COPY ["src/AuthSmith.Contracts/AuthSmith.Contracts.csproj", "src/AuthSmith.Contracts/"]
COPY ["src/AuthSmith.Domain/AuthSmith.Domain.csproj", "src/AuthSmith.Domain/"]
COPY ["src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj", "src/AuthSmith.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/AuthSmith.Api/AuthSmith.Api.csproj"

# Copy everything else
COPY . .

# Build and publish
WORKDIR "/src/src/AuthSmith.Api"
RUN dotnet publish "AuthSmith.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:Version=${VERSION} \
    /p:InformationalVersion="${VERSION}+${BUILD_NUMBER}.${COMMIT_HASH}"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r authsmith && useradd -r -g authsmith authsmith

# Copy published app
COPY --from=build /app/publish .

# Create directories for keys and logs
RUN mkdir -p /app/keys /app/logs && \
    chown -R authsmith:authsmith /app

# Switch to non-root user
USER authsmith

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Environment variables with defaults
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__AutoMigrate=false

ENTRYPOINT ["dotnet", "AuthSmith.Api.dll"]

