# Caskr Server Production Dockerfile
# Multi-stage build for optimal image size
#
# If you have issues pulling from MCR (Microsoft Container Registry),
# try these commands first to pull images manually:
#   docker pull mcr.microsoft.com/dotnet/sdk:8.0-alpine
#   docker pull mcr.microsoft.com/dotnet/aspnet:8.0-alpine
#   docker pull node:20-alpine

# Stage 1: Build Frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend
COPY caskr.client/package*.json ./
RUN npm ci --production=false
COPY caskr.client/ ./
ENV VITE_USE_HTTPS=false
ENV VITE_DISABLE_PROXY=true
RUN npm run build

# Stage 2: Build Backend
# Using Alpine SDK - significantly smaller (~500MB vs ~900MB), more reliable to download
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend-build
WORKDIR /src

# Install ICU for globalization support (required for some .NET features on Alpine)
RUN apk add --no-cache icu-libs

# Set environment to use ICU
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy only the server project file (frontend is built separately in Stage 1)
COPY Caskr.Server/Caskr.server.csproj ./Caskr.Server/

# Restore dependencies
RUN dotnet restore Caskr.Server/Caskr.server.csproj --runtime linux-musl-x64

# Copy everything else and build
COPY Caskr.Server/ ./Caskr.Server/

# Copy frontend build output to wwwroot
COPY --from=frontend-build /app/frontend/dist ./Caskr.Server/wwwroot/

# Build and publish (self-contained for Alpine)
RUN dotnet publish Caskr.Server/Caskr.server.csproj -c Release -o /app/publish --no-restore --runtime linux-musl-x64 --self-contained false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks and ICU for globalization
RUN apk add --no-cache curl icu-libs

# Enable globalization support
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user for security
RUN addgroup -g 1000 caskr && adduser -u 1000 -G caskr -s /bin/sh -D caskr

# Create log directory with proper permissions
RUN mkdir -p /var/log/caskr && chown -R caskr:caskr /var/log/caskr

USER caskr

# Copy published application
COPY --from=backend-build --chown=caskr:caskr /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "Caskr.server.dll"]
