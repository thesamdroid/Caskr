#!/bin/bash
set -e

# Caskr Production Deployment Script
# Usage: ./scripts/deploy-production.sh

echo "======================================"
echo "Caskr Production Deployment"
echo "======================================"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check for .env file
if [ ! -f .env ]; then
    echo -e "${RED}Error: .env file not found${NC}"
    echo "Please copy .env.production.example to .env and configure it"
    exit 1
fi

# Validate required environment variables
source .env

required_vars=("POSTGRES_PASSWORD" "JWT_SECRET_KEY" "KEYCLOAK_ADMIN_PASSWORD")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}Error: $var is not set in .env${NC}"
        exit 1
    fi
done

echo -e "${GREEN}Environment configuration validated${NC}"

# Pull latest images (if using a registry)
if [ -n "$DOCKER_REGISTRY" ]; then
    echo -e "${YELLOW}Pulling latest images from registry...${NC}"
    docker-compose -f docker-compose.production.yml pull
fi

# Backup database before deployment
echo -e "${YELLOW}Creating database backup...${NC}"
BACKUP_FILE="backup_$(date +%Y%m%d_%H%M%S).sql"
docker exec caskr-db pg_dump -U ${POSTGRES_USER:-postgres} ${POSTGRES_DB:-caskr-db} > ./backups/$BACKUP_FILE 2>/dev/null || echo "No existing database to backup"

# Deploy with zero-downtime
echo -e "${YELLOW}Deploying services...${NC}"
docker-compose -f docker-compose.production.yml up -d --build

# Wait for services to be healthy
echo -e "${YELLOW}Waiting for services to be healthy...${NC}"
sleep 10

# Check health
echo -e "${YELLOW}Checking service health...${NC}"

# Check server health
if curl -sf http://localhost:8081/api/health > /dev/null; then
    echo -e "${GREEN}Server is healthy${NC}"
else
    echo -e "${RED}Server health check failed${NC}"
    docker-compose -f docker-compose.production.yml logs caskr-server
    exit 1
fi

# Check database
if docker exec caskr-db pg_isready -U ${POSTGRES_USER:-postgres} > /dev/null 2>&1; then
    echo -e "${GREEN}Database is healthy${NC}"
else
    echo -e "${RED}Database health check failed${NC}"
    exit 1
fi

echo ""
echo "======================================"
echo -e "${GREEN}Deployment Complete!${NC}"
echo "======================================"
echo ""
echo "Services running:"
docker-compose -f docker-compose.production.yml ps
echo ""
echo "Application URL: https://caskr.co"
echo "API URL: https://api.caskr.co"
echo "Auth URL: https://auth.caskr.co"
