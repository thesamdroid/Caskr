#!/bin/bash
set -e

# Caskr Local Demo Setup Script
# Usage: ./scripts/local-demo.sh [start|stop|restart|status|logs|clean]

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

COMPOSE_FILE="docker-compose.local.yml"
PROJECT_NAME="caskr-local"

print_banner() {
    echo -e "${BLUE}"
    echo "╔═══════════════════════════════════════════════╗"
    echo "║           Caskr Local Demo Setup              ║"
    echo "╚═══════════════════════════════════════════════╝"
    echo -e "${NC}"
}

check_prerequisites() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"

    # Check Docker
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Error: Docker is not installed${NC}"
        echo "Please install Docker: https://docs.docker.com/get-docker/"
        exit 1
    fi
    echo -e "  ${GREEN}✓${NC} Docker installed"

    # Check Docker Compose
    if ! docker compose version &> /dev/null; then
        echo -e "${RED}Error: Docker Compose is not installed${NC}"
        echo "Please install Docker Compose: https://docs.docker.com/compose/install/"
        exit 1
    fi
    echo -e "  ${GREEN}✓${NC} Docker Compose installed"

    # Check if Docker is running
    if ! docker info &> /dev/null; then
        echo -e "${RED}Error: Docker daemon is not running${NC}"
        echo "Please start Docker and try again"
        exit 1
    fi
    echo -e "  ${GREEN}✓${NC} Docker daemon running"

    echo ""
}

build_images() {
    echo -e "${YELLOW}Building Docker images...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME build
    echo -e "${GREEN}Build complete!${NC}"
    echo ""
}

start_services() {
    echo -e "${YELLOW}Starting services...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME up -d

    echo ""
    echo -e "${YELLOW}Waiting for services to be healthy...${NC}"

    # Wait for database
    echo -n "  Database: "
    timeout=60
    while [ $timeout -gt 0 ]; do
        if docker compose -f $COMPOSE_FILE -p $PROJECT_NAME exec -T caskr-db pg_isready -U postgres -d caskr-db &> /dev/null; then
            echo -e "${GREEN}Ready${NC}"
            break
        fi
        sleep 1
        ((timeout--))
    done
    if [ $timeout -eq 0 ]; then
        echo -e "${RED}Timeout${NC}"
    fi

    # Wait for Keycloak
    echo -n "  Keycloak: "
    timeout=90
    while [ $timeout -gt 0 ]; do
        if curl -sf http://localhost:8080/health/ready &> /dev/null; then
            echo -e "${GREEN}Ready${NC}"
            break
        fi
        sleep 2
        ((timeout-=2))
    done
    if [ $timeout -le 0 ]; then
        echo -e "${YELLOW}Starting (may take a minute)${NC}"
    fi

    # Wait for application server
    echo -n "  Server:   "
    timeout=120
    while [ $timeout -gt 0 ]; do
        if curl -sf http://localhost:5000/api/health &> /dev/null; then
            echo -e "${GREEN}Ready${NC}"
            break
        fi
        sleep 2
        ((timeout-=2))
    done
    if [ $timeout -le 0 ]; then
        echo -e "${YELLOW}Starting (may take a minute)${NC}"
    fi

    echo ""
}

stop_services() {
    echo -e "${YELLOW}Stopping services...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME down
    echo -e "${GREEN}Services stopped${NC}"
}

show_status() {
    echo -e "${YELLOW}Service Status:${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME ps
}

show_logs() {
    SERVICE=$1
    if [ -n "$SERVICE" ]; then
        docker compose -f $COMPOSE_FILE -p $PROJECT_NAME logs -f $SERVICE
    else
        docker compose -f $COMPOSE_FILE -p $PROJECT_NAME logs -f
    fi
}

clean_all() {
    echo -e "${YELLOW}Cleaning up all containers and volumes...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME down -v --rmi local
    echo -e "${GREEN}Cleanup complete${NC}"
}

print_access_info() {
    echo -e "${GREEN}"
    echo "╔═══════════════════════════════════════════════════════════════╗"
    echo "║                    Demo is Ready!                             ║"
    echo "╠═══════════════════════════════════════════════════════════════╣"
    echo "║                                                               ║"
    echo "║  Application:  http://localhost:5000                          ║"
    echo "║  Keycloak:     http://localhost:8080                          ║"
    echo "║  Database:     localhost:5432                                 ║"
    echo "║                                                               ║"
    echo "╠═══════════════════════════════════════════════════════════════╣"
    echo "║  Demo Credentials:                                            ║"
    echo "║    Username: shaw@caskr.co                                    ║"
    echo "║    Password: Whiskey123!                                      ║"
    echo "║                                                               ║"
    echo "║  Keycloak Admin:                                              ║"
    echo "║    Username: admin                                            ║"
    echo "║    Password: admin123                                         ║"
    echo "║                                                               ║"
    echo "║  Database:                                                    ║"
    echo "║    User: postgres                                             ║"
    echo "║    Password: localdev123                                      ║"
    echo "╚═══════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# Main script
print_banner

case "${1:-start}" in
    start)
        check_prerequisites
        build_images
        start_services
        print_access_info
        ;;
    stop)
        stop_services
        ;;
    restart)
        stop_services
        start_services
        print_access_info
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs $2
        ;;
    clean)
        clean_all
        ;;
    build)
        check_prerequisites
        build_images
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|logs [service]|clean|build}"
        echo ""
        echo "Commands:"
        echo "  start   - Build and start all services (default)"
        echo "  stop    - Stop all services"
        echo "  restart - Restart all services"
        echo "  status  - Show service status"
        echo "  logs    - Show logs (optionally specify service)"
        echo "  clean   - Remove all containers, volumes, and images"
        echo "  build   - Build images without starting"
        exit 1
        ;;
esac
