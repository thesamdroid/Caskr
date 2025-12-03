# Caskr Deployment Guide

This guide covers deploying Caskr to production at caskr.co and running locally for development.

## Architecture Overview

Caskr consists of the following components:

- **Caskr Server**: ASP.NET Core 8.0 API + React SPA
- **PostgreSQL**: Database (postgres:17)
- **Keycloak**: Authentication and identity management
- **Nginx**: Reverse proxy with SSL termination (production only)

## Quick Start - Local Development

### Prerequisites
- Docker and Docker Compose
- Git

### Run Locally

```bash
# Clone and start all services
git clone <repository>
cd Caskr

# Start the local environment
docker-compose -f docker-compose.local.yml up --build

# Access the application
# - App: http://localhost:5000
# - Keycloak Admin: http://localhost:8080 (admin/admin123)
# - Database: localhost:5432 (postgres/localdev123)
```

## Production Deployment (caskr.co)

### Prerequisites
- Docker and Docker Compose
- SSL certificates for caskr.co (wildcard or individual certs)
- DNS configured for caskr.co, api.caskr.co, auth.caskr.co

### Step 1: Configure Environment

```bash
# Copy the production environment template
cp .env.production.example .env

# Edit with your production values
nano .env
```

Required environment variables:
- `POSTGRES_PASSWORD`: Strong database password
- `JWT_SECRET_KEY`: 64+ character secret for JWT signing
- `KEYCLOAK_ADMIN_PASSWORD`: Keycloak admin password
- `SENDGRID_API_KEY`: (Optional) For email functionality

### Step 2: Configure SSL Certificates

Place your SSL certificates in the `nginx/ssl/` directory:
- `caskr.co.crt`: SSL certificate (or fullchain)
- `caskr.co.key`: Private key

For Let's Encrypt:
```bash
# Install certbot and obtain certificates
certbot certonly --webroot -w /var/www/certbot \
  -d caskr.co -d www.caskr.co -d api.caskr.co -d auth.caskr.co
```

### Step 3: Build Docker Images

```bash
# Build all images
./scripts/build-docker-images.sh latest

# Or build and push to a registry
./scripts/build-docker-images.sh v1.0.0 your-registry.example.com
```

### Step 4: Deploy

```bash
# Deploy to production
./scripts/deploy-production.sh
```

### Step 5: Verify Deployment

```bash
# Check service status
docker-compose -f docker-compose.production.yml ps

# Check logs
docker-compose -f docker-compose.production.yml logs -f

# Health check
curl https://caskr.co/api/health
```

## Docker Images

| Image | Description | Port |
|-------|-------------|------|
| caskr/server | Main app (API + SPA) | 8080 |
| caskr/database | PostgreSQL | 5432 |
| caskr/keycloak | Authentication | 8080 |

## URLs

### Production
- **Application**: https://caskr.co
- **API**: https://api.caskr.co or https://caskr.co/api
- **Authentication**: https://auth.caskr.co
- **Swagger Docs**: https://caskr.co/swagger (dev only)

### Local Development
- **Application**: http://localhost:5000
- **Keycloak**: http://localhost:8080
- **Database**: localhost:5432

## Database Migrations

Database migrations are automatically applied via init scripts in `Database/initdb.d/`. For manual migrations:

```bash
# Connect to database
docker exec -it caskr-db psql -U postgres -d caskr-db

# Run a migration file
docker exec -i caskr-db psql -U postgres -d caskr-db < Database/initdb.d/XX-migration.sql
```

## Backup and Restore

### Backup
```bash
docker exec caskr-db pg_dump -U postgres caskr-db > backup_$(date +%Y%m%d).sql
```

### Restore
```bash
docker exec -i caskr-db psql -U postgres caskr-db < backup_YYYYMMDD.sql
```

## Monitoring

### Health Endpoints
- `GET /api/health` - Basic health check
- `GET /api/health/detailed` - Detailed health with database status

### Logs
```bash
# All services
docker-compose -f docker-compose.production.yml logs -f

# Specific service
docker-compose -f docker-compose.production.yml logs -f caskr-server
```

## Troubleshooting

### Server won't start
1. Check logs: `docker-compose logs caskr-server`
2. Verify environment variables in `.env`
3. Ensure database is healthy: `docker exec caskr-db pg_isready`

### Database connection issues
1. Check database is running: `docker-compose ps caskr-db`
2. Verify connection string matches environment
3. Check database logs: `docker-compose logs caskr-db`

### SSL/Certificate issues
1. Verify certificates exist in `nginx/ssl/`
2. Check nginx config: `docker exec caskr-nginx nginx -t`
3. Review nginx logs: `docker-compose logs nginx`

## Security Considerations

1. **Never commit `.env` files** - Use `.env.example` as templates
2. **Rotate secrets regularly** - Especially JWT keys and database passwords
3. **Use strong passwords** - Minimum 32 characters for JWT keys
4. **Enable HTTPS** - Always use SSL in production
5. **Restrict database access** - Only allow connections from app containers

## Support

For issues and feature requests, contact the development team.
