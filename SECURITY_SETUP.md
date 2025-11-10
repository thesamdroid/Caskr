# Security Configuration Setup

## Overview

Sensitive configuration values have been removed from appsettings.json files. You need to configure these secrets using one of the following methods:

## Option 1: User Secrets (Recommended for Local Development)

1. Navigate to the Server project directory:
   ```bash
   cd Caskr.Server
   ```

2. Initialize user secrets:
   ```bash
   dotnet user-secrets init
   ```

3. Set each secret individually:
   ```bash
   dotnet user-secrets set "ConnectionStrings:CaskrDatabaseConnectionString" "Host=localhost; Port=5432; Database=caskr-db; Username=postgres; Password=docker"
   dotnet user-secrets set "SendGrid:ApiKey" "your-sendgrid-api-key"
   dotnet user-secrets set "Jwt:Key" "your-jwt-secret-key-at-least-32-characters"
   dotnet user-secrets set "Keycloak:ClientSecret" "your-keycloak-client-secret"
   dotnet user-secrets set "Keycloak:AdminUser" "your-admin-email"
   dotnet user-secrets set "Keycloak:AdminPassword" "your-admin-password"
   ```

## Option 2: Environment Variables

Set the following environment variables:

```bash
# Windows (PowerShell)
$env:ConnectionStrings__CaskrDatabaseConnectionString="Host=localhost; Port=5432; Database=caskr-db; Username=postgres; Password=docker"
$env:SendGrid__ApiKey="your-sendgrid-api-key"
$env:Jwt__Key="your-jwt-secret-key-at-least-32-characters"
$env:Keycloak__ClientSecret="your-keycloak-client-secret"
$env:Keycloak__AdminUser="your-admin-email"
$env:Keycloak__AdminPassword="your-admin-password"

# Linux/macOS
export ConnectionStrings__CaskrDatabaseConnectionString="Host=localhost; Port=5432; Database=caskr-db; Username=postgres; Password=docker"
export SendGrid__ApiKey="your-sendgrid-api-key"
export Jwt__Key="your-jwt-secret-key-at-least-32-characters"
export Keycloak__ClientSecret="your-keycloak-client-secret"
export Keycloak__AdminUser="your-admin-email"
export Keycloak__AdminPassword="your-admin-password"
```

## Option 3: Local Secrets File (Not recommended - for quick testing only)

1. Copy the example secrets file:
   ```bash
   cp Caskr.Server/appsettings.secrets.example.json Caskr.Server/appsettings.secrets.json
   ```

2. Edit `appsettings.secrets.json` with your actual values

3. Update `Program.cs` to load this file (already configured in the builder)

**Note:** This file is gitignored and will not be committed to source control.

## Option 4: Azure Key Vault (Recommended for Production)

For production deployments, use Azure Key Vault:

1. Create an Azure Key Vault
2. Add your secrets to the vault
3. Configure the application to read from Key Vault in `Program.cs`
4. Use Managed Identity for authentication

## Docker Compose Environment Variables

Update your `docker-compose.yml` to use environment variables or secrets:

```yaml
services:
  app:
    environment:
      - ConnectionStrings__CaskrDatabaseConnectionString=${DB_CONNECTION_STRING}
      - Jwt__Key=${JWT_SECRET_KEY}
      # ... other variables
```

Create a `.env` file in the same directory as `docker-compose.yml` (see `.env.example`).

## Security Best Practices

1. **Never commit secrets to source control**
2. **Use different secrets for each environment** (dev, staging, production)
3. **Rotate secrets regularly**
4. **Use strong, randomly generated keys** (at least 32 characters for JWT keys)
5. **Limit access** to production secrets to only necessary personnel
6. **Audit secret access** in production environments

## Generating Secure Keys

For JWT keys, generate a secure random string:

```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})

# Linux/macOS
openssl rand -base64 32
```
