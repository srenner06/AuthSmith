# Docker Setup

This directory contains Docker configuration files for running AuthSmith in containers.

## Files

- `Dockerfile` - Multi-stage Docker build for the AuthSmith API
- `docker-compose.yml` - Complete development environment with API, PostgreSQL, and Redis

## Quick Start

### Development Environment

Start all services (API, PostgreSQL, Redis):

```bash
docker-compose -f docker/docker-compose.yml up -d
```

The API will be available at `http://localhost:8080`

### Building the Image

Build the Docker image:

```bash
docker build -f docker/Dockerfile -t authsmith:latest .
```

### Running the Container

Run the container with environment variables:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=authsmith;Username=postgres;Password=postgres" \
  -e Database__AutoMigrate=true \
  authsmith:latest
```

## Environment Variables

Key environment variables for the API container:

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Database__AutoMigrate` - Set to `true` to auto-apply migrations on startup
- `Jwt__PrivateKeyPath` - Path to JWT private key (mount as volume if needed)
- `Redis__Enabled` - Set to `true` to enable Redis caching
- `Redis__ConnectionString` - Redis connection string
- `ApiKeys__AdminKey` - Admin API key for initial setup
- `ASPNETCORE_ENVIRONMENT` - Environment (Development, Production, etc.)

## Pulling from GitHub Container Registry

Images are published to GitHub Container Registry (ghcr.io). To pull:

```bash
# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Pull the image
docker pull ghcr.io/OWNER/authsmith:latest
```

Replace `OWNER` with your GitHub username or organization.

## Production Considerations

For production deployments:

1. **Secrets Management**: Use Docker secrets or environment variable files
2. **Health Checks**: Configure proper health check endpoints
3. **Resource Limits**: Set appropriate CPU and memory limits
4. **Networking**: Use proper network isolation
5. **Volumes**: Mount JWT keys and configuration securely
6. **Logging**: Configure centralized logging

## Docker Compose Services

- **api**: AuthSmith API service
- **postgres**: PostgreSQL 16 database
- **redis**: Redis 7 cache server

All services are connected via the `authsmith-network` bridge network.

