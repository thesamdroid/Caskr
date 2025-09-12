#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Start PostgreSQL in the background using the image's default entrypoint
Start-Process -FilePath '/docker-entrypoint.sh' -ArgumentList 'postgres'

# Wait for PostgreSQL to be ready to accept connections
while (-not (pg_isready -h localhost -U $env:POSTGRES_USER *> $null)) {
    Start-Sleep -Seconds 1
}

# Start Keycloak in the foreground
& /opt/keycloak/bin/kc.sh start-dev --http-port=8080 --hostname-strict=false
