$ErrorActionPreference = 'Stop'

Write-Host "Stopping existing database container..."
docker compose -f "$PSScriptRoot/docker-compose.yml" down --volumes

$localDataPath = Join-Path $PSScriptRoot 'postgres'
if (Test-Path $localDataPath) {
    Write-Host "Removing local database data at $localDataPath"
    Remove-Item -Recurse -Force $localDataPath
}

Write-Host "Starting database container with fresh schema and test data..."
docker compose -f "$PSScriptRoot/docker-compose.yml" up -d

Write-Host "Database rebuild complete."
