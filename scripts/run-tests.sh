#!/bin/bash
set -e

# Caskr Test Runner Script
# Runs tests via Docker to bypass local network/proxy issues
# Usage: ./scripts/run-tests.sh [dotnet|frontend|all]

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

COMPOSE_FILE="docker-compose.test.yml"
PROJECT_NAME="caskr-test"

print_banner() {
    echo -e "${BLUE}"
    echo "╔═══════════════════════════════════════════════╗"
    echo "║             Caskr Test Runner                 ║"
    echo "╚═══════════════════════════════════════════════╝"
    echo -e "${NC}"
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Error: Docker is not installed${NC}"
        exit 1
    fi
    if ! docker info &> /dev/null; then
        echo -e "${RED}Error: Docker daemon is not running${NC}"
        exit 1
    fi
}

build_test_runner() {
    echo -e "${YELLOW}Building test runner image...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME build test-runner
    echo -e "${GREEN}Build complete!${NC}"
    echo ""
}

start_test_db() {
    echo -e "${YELLOW}Starting test database...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME up -d test-db

    # Wait for database to be healthy
    echo -n "  Waiting for database: "
    timeout=60
    while [ $timeout -gt 0 ]; do
        if docker compose -f $COMPOSE_FILE -p $PROJECT_NAME exec -T test-db pg_isready -U postgres -d caskr-test-db &> /dev/null; then
            echo -e "${GREEN}Ready${NC}"
            break
        fi
        sleep 1
        ((timeout--))
    done
    if [ $timeout -eq 0 ]; then
        echo -e "${RED}Timeout${NC}"
        exit 1
    fi
    echo ""
}

run_dotnet_tests() {
    echo -e "${YELLOW}Running .NET tests...${NC}"
    echo ""

    # Create test-results directory if it doesn't exist
    mkdir -p test-results

    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME run --rm test-runner \
        dotnet test --configuration Release --verbosity normal \
        --logger "trx;LogFileName=/workspace/test-results/dotnet-results.trx" \
        --collect:"XPlat Code Coverage" \
        --results-directory /workspace/test-results

    local exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✓ .NET tests passed!${NC}"
    else
        echo ""
        echo -e "${RED}✗ .NET tests failed!${NC}"
    fi

    return $exit_code
}

run_frontend_tests() {
    echo -e "${YELLOW}Running frontend tests...${NC}"
    echo ""

    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME run --rm test-runner \
        bash -c "cd caskr.client && npm run build && npm test -- --run"

    local exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✓ Frontend tests passed!${NC}"
    else
        echo ""
        echo -e "${RED}✗ Frontend tests failed!${NC}"
    fi

    return $exit_code
}

cleanup() {
    echo -e "${YELLOW}Cleaning up...${NC}"
    docker compose -f $COMPOSE_FILE -p $PROJECT_NAME down -v
    echo -e "${GREEN}Cleanup complete${NC}"
}

show_usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  dotnet    - Run .NET server tests only"
    echo "  frontend  - Run frontend tests only"
    echo "  all       - Run all tests (default)"
    echo "  build     - Build test runner image only"
    echo "  clean     - Remove test containers and volumes"
    echo ""
    echo "Examples:"
    echo "  $0              # Run all tests"
    echo "  $0 dotnet       # Run only .NET tests"
    echo "  $0 frontend     # Run only frontend tests"
}

# Main script
print_banner
check_docker

case "${1:-all}" in
    dotnet)
        build_test_runner
        start_test_db
        run_dotnet_tests
        exit_code=$?
        cleanup
        exit $exit_code
        ;;
    frontend)
        build_test_runner
        run_frontend_tests
        exit_code=$?
        cleanup
        exit $exit_code
        ;;
    all)
        build_test_runner
        start_test_db

        dotnet_exit=0
        frontend_exit=0

        run_dotnet_tests || dotnet_exit=$?
        echo ""
        run_frontend_tests || frontend_exit=$?

        cleanup

        echo ""
        echo -e "${BLUE}═══════════════════════════════════════════════${NC}"
        echo -e "${BLUE}                Test Summary                    ${NC}"
        echo -e "${BLUE}═══════════════════════════════════════════════${NC}"

        if [ $dotnet_exit -eq 0 ]; then
            echo -e "  .NET Tests:     ${GREEN}PASSED${NC}"
        else
            echo -e "  .NET Tests:     ${RED}FAILED${NC}"
        fi

        if [ $frontend_exit -eq 0 ]; then
            echo -e "  Frontend Tests: ${GREEN}PASSED${NC}"
        else
            echo -e "  Frontend Tests: ${RED}FAILED${NC}"
        fi

        echo ""

        if [ $dotnet_exit -eq 0 ] && [ $frontend_exit -eq 0 ]; then
            echo -e "${GREEN}All tests passed!${NC}"
            exit 0
        else
            echo -e "${RED}Some tests failed!${NC}"
            exit 1
        fi
        ;;
    build)
        build_test_runner
        ;;
    clean)
        cleanup
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        echo ""
        show_usage
        exit 1
        ;;
esac
