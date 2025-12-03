#!/bin/bash
set -e

# Caskr Docker Image Build Script
# Usage: ./scripts/build-docker-images.sh [tag] [registry]

TAG=${1:-latest}
REGISTRY=${2:-}

echo "======================================"
echo "Building Caskr Docker Images"
echo "Tag: $TAG"
echo "Registry: ${REGISTRY:-local}"
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Build Database Image
echo -e "${YELLOW}Building Database Image...${NC}"
docker build -t caskr/database:$TAG ./Database
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Database image built successfully${NC}"
else
    echo -e "${RED}Failed to build database image${NC}"
    exit 1
fi

# Build Keycloak Image
echo -e "${YELLOW}Building Keycloak Image...${NC}"
docker build -t caskr/keycloak:$TAG ./Keycloak
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Keycloak image built successfully${NC}"
else
    echo -e "${RED}Failed to build keycloak image${NC}"
    exit 1
fi

# Build Server Image (includes frontend)
echo -e "${YELLOW}Building Server Image (with Frontend)...${NC}"
docker build -t caskr/server:$TAG -f Caskr.Server/Dockerfile .
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Server image built successfully${NC}"
else
    echo -e "${RED}Failed to build server image${NC}"
    exit 1
fi

# Tag and push if registry is specified
if [ -n "$REGISTRY" ]; then
    echo -e "${YELLOW}Tagging and pushing images to $REGISTRY...${NC}"

    docker tag caskr/database:$TAG $REGISTRY/caskr/database:$TAG
    docker tag caskr/keycloak:$TAG $REGISTRY/caskr/keycloak:$TAG
    docker tag caskr/server:$TAG $REGISTRY/caskr/server:$TAG

    docker push $REGISTRY/caskr/database:$TAG
    docker push $REGISTRY/caskr/keycloak:$TAG
    docker push $REGISTRY/caskr/server:$TAG

    echo -e "${GREEN}Images pushed to registry${NC}"
fi

echo ""
echo "======================================"
echo -e "${GREEN}Build Complete!${NC}"
echo "======================================"
echo ""
echo "Images built:"
echo "  - caskr/database:$TAG"
echo "  - caskr/keycloak:$TAG"
echo "  - caskr/server:$TAG"
echo ""
echo "To run locally:"
echo "  docker-compose -f docker-compose.local.yml up"
echo ""
echo "To run in production:"
echo "  cp .env.production.example .env"
echo "  # Edit .env with your production values"
echo "  docker-compose -f docker-compose.production.yml up -d"
