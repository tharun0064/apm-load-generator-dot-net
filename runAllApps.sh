#!/bin/bash

# Script to run all load generator applications
# Usage: ./runAllApps.sh         - Start all apps in background
#        ./runAllApps.sh --stop   - Stop all apps
#        ./runAllApps.sh --status - Check status of all apps

set -e

APP1_DIR="App1OltpLoadGenerator"
APP2_DIR="App2AnalyticsLoadGenerator"
APP3_DIR="App3AnalyticsLoadGenerator"
APP4_DIR="App4AnalyticsLoadGeneratorNoApm"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Repo root (this script's directory), used to locate the New Relic helper
ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Stop mode
if [[ "$1" == "--stop" ]]; then
    echo "=========================================="
    echo "Stopping All Applications"
    echo "=========================================="

    echo -e "${YELLOW}Stopping App1 (OLTP)...${NC}"
    pkill -f 'dotnet.*App1OltpLoadGenerator' && echo -e "${GREEN}✓ App1 stopped${NC}" || echo -e "${YELLOW}⚠ App1 not running${NC}"

    echo -e "${YELLOW}Stopping App2 (Analytics)...${NC}"
    pkill -f 'dotnet.*App2AnalyticsLoadGenerator' && echo -e "${GREEN}✓ App2 stopped${NC}" || echo -e "${YELLOW}⚠ App2 not running${NC}"

    echo -e "${YELLOW}Stopping App3 (Analytics3)...${NC}"
    pkill -f 'dotnet.*App3AnalyticsLoadGenerator' && echo -e "${GREEN}✓ App3 stopped${NC}" || echo -e "${YELLOW}⚠ App3 not running${NC}"

    echo -e "${YELLOW}Stopping App4 (Analytics No APM)...${NC}"
    pkill -f 'dotnet.*App4AnalyticsLoadGeneratorNoApm' && echo -e "${GREEN}✓ App4 stopped${NC}" || echo -e "${YELLOW}⚠ App4 not running${NC}"

    echo "=========================================="
    exit 0
fi

# Status mode
if [[ "$1" == "--status" ]]; then
    echo "=========================================="
    echo "Application Status"
    echo "=========================================="

    APP1_PID=$(pgrep -f 'dotnet.*App1OltpLoadGenerator' || echo "")
    APP2_PID=$(pgrep -f 'dotnet.*App2AnalyticsLoadGenerator' || echo "")
    APP3_PID=$(pgrep -f 'dotnet.*App3AnalyticsLoadGenerator' || echo "")
    APP4_PID=$(pgrep -f 'dotnet.*App4AnalyticsLoadGeneratorNoApm' || echo "")

    if [ -n "$APP1_PID" ]; then
        echo -e "${GREEN}✓ App1 (OLTP) running (PID: $APP1_PID, Port: 8080)${NC}"
    else
        echo -e "${RED}✗ App1 (OLTP) not running${NC}"
    fi

    if [ -n "$APP2_PID" ]; then
        echo -e "${GREEN}✓ App2 (Analytics) running (PID: $APP2_PID, Port: 8081)${NC}"
    else
        echo -e "${RED}✗ App2 (Analytics) not running${NC}"
    fi

    if [ -n "$APP3_PID" ]; then
        echo -e "${GREEN}✓ App3 (Analytics3) running (PID: $APP3_PID, Port: 8082)${NC}"
    else
        echo -e "${RED}✗ App3 (Analytics3) not running${NC}"
    fi

    if [ -n "$APP4_PID" ]; then
        echo -e "${GREEN}✓ App4 (Analytics No APM) running (PID: $APP4_PID, Port: 8083)${NC}"
    else
        echo -e "${RED}✗ App4 (Analytics No APM) not running${NC}"
    fi

    echo "=========================================="
    exit 0
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: .NET SDK is not installed${NC}"
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if apps are already running
echo "=========================================="
echo "Checking for Running Applications"
echo "=========================================="

APP1_RUNNING=$(pgrep -f 'dotnet.*App1OltpLoadGenerator' || echo "")
APP2_RUNNING=$(pgrep -f 'dotnet.*App2AnalyticsLoadGenerator' || echo "")
APP3_RUNNING=$(pgrep -f 'dotnet.*App3AnalyticsLoadGenerator' || echo "")
APP4_RUNNING=$(pgrep -f 'dotnet.*App4AnalyticsLoadGeneratorNoApm' || echo "")

if [ -n "$APP1_RUNNING" ] || [ -n "$APP2_RUNNING" ] || [ -n "$APP3_RUNNING" ] || [ -n "$APP4_RUNNING" ]; then
    echo -e "${YELLOW}Found running applications:${NC}"
    [ -n "$APP1_RUNNING" ] && echo "  App1 (OLTP): PID $APP1_RUNNING"
    [ -n "$APP2_RUNNING" ] && echo "  App2 (Analytics): PID $APP2_RUNNING"
    [ -n "$APP3_RUNNING" ] && echo "  App3 (Analytics3): PID $APP3_RUNNING"
    [ -n "$APP4_RUNNING" ] && echo "  App4 (Analytics No APM): PID $APP4_RUNNING"
    echo ""
    echo -e "${YELLOW}Stopping existing processes...${NC}"

    [ -n "$APP1_RUNNING" ] && pkill -f 'dotnet.*App1OltpLoadGenerator' && echo -e "${GREEN}✓ Stopped App1${NC}"
    [ -n "$APP2_RUNNING" ] && pkill -f 'dotnet.*App2AnalyticsLoadGenerator' && echo -e "${GREEN}✓ Stopped App2${NC}"
    [ -n "$APP3_RUNNING" ] && pkill -f 'dotnet.*App3AnalyticsLoadGenerator' && echo -e "${GREEN}✓ Stopped App3${NC}"
    [ -n "$APP4_RUNNING" ] && pkill -f 'dotnet.*App4AnalyticsLoadGeneratorNoApm' && echo -e "${GREEN}✓ Stopped App4${NC}"

    # Wait for processes to fully terminate
    sleep 2
fi

echo -e "${GREEN}✓ No running applications${NC}"
echo ""

# Check if .env files exist
echo "=========================================="
echo "Verifying Configuration Files"
echo "=========================================="

MISSING_ENV=0

if [ ! -f "$APP1_DIR/.env" ]; then
    echo -e "${RED}✗ $APP1_DIR/.env not found${NC}"
    echo "  Copy .env.example to .env and configure it"
    MISSING_ENV=1
fi

if [ ! -f "$APP2_DIR/.env" ]; then
    echo -e "${RED}✗ $APP2_DIR/.env not found${NC}"
    echo "  Copy .env.example to .env and configure it"
    MISSING_ENV=1
fi

if [ ! -f "$APP3_DIR/.env" ]; then
    echo -e "${RED}✗ $APP3_DIR/.env not found${NC}"
    echo "  Copy .env.example to .env and configure it"
    MISSING_ENV=1
fi

if [ ! -f "$APP4_DIR/.env" ]; then
    echo -e "${RED}✗ $APP4_DIR/.env not found${NC}"
    echo "  Copy .env.example to .env and configure it"
    MISSING_ENV=1
fi

if [ $MISSING_ENV -eq 1 ]; then
    echo ""
    echo -e "${YELLOW}To create .env files quickly:${NC}"
    echo "  cd $APP1_DIR && cp .env.example .env"
    echo "  cd ../$APP2_DIR && cp .env.example .env"
    echo "  cd ../$APP3_DIR && cp .env.example .env"
    echo "  cd ../$APP4_DIR && cp .env.example .env"
    echo ""
    echo "Then edit each .env file with your database credentials and New Relic license key."
    exit 1
fi

echo -e "${GREEN}✓ All .env files found${NC}"
echo ""

# Helper: build an app, then launch it in an isolated subshell so the New Relic
# profiler environment variables never leak between apps. Pass apm=yes to attach
# the agent (Apps 1-3); apm=no leaves the app uninstrumented (App4 baseline).
# Usage: start_app <dir> <port> <yes|no>
start_app() {
    local dir="$1" port="$2" apm="$3"
    echo "=========================================="
    echo "Starting $dir (APM: $apm)"
    echo "=========================================="

    if ( cd "$ROOT_DIR/$dir" && dotnet build > /dev/null 2>&1 ); then
        (
            cd "$ROOT_DIR/$dir"
            [ -f .env ] && export $(cat .env | grep -v '^#' | xargs)
            if [ "$apm" = "yes" ]; then
                source "$ROOT_DIR/newrelic-env.sh"
                enable_newrelic "$PWD" || true
            fi
            exec dotnet run --no-build > /dev/null 2>&1
        ) &
        local pid=$!
        echo -e "${BLUE}Started $dir with PID: $pid (Port: $port)${NC}"
    else
        echo -e "${RED}✗ Build failed for $dir — not started${NC}"
    fi
    echo ""
}

# Apps 1-3 are instrumented with New Relic; App4 is the no-APM baseline.
start_app "$APP1_DIR" 8080 yes
sleep 3
start_app "$APP2_DIR" 8081 yes
sleep 3
start_app "$APP3_DIR" 8082 yes
sleep 3
start_app "$APP4_DIR" 8083 no

# Wait for apps to start
echo "Waiting for applications to initialize..."
sleep 5

# Verify all apps are running
echo "=========================================="
echo "Verification"
echo "=========================================="

APP1_CHECK=$(pgrep -f 'dotnet.*App1OltpLoadGenerator' || echo "")
APP2_CHECK=$(pgrep -f 'dotnet.*App2AnalyticsLoadGenerator' || echo "")
APP3_CHECK=$(pgrep -f 'dotnet.*App3AnalyticsLoadGenerator' || echo "")
APP4_CHECK=$(pgrep -f 'dotnet.*App4AnalyticsLoadGeneratorNoApm' || echo "")

FAILED=0

if [ -n "$APP1_CHECK" ]; then
    echo -e "${GREEN}✓ App1 running (PID: $APP1_CHECK) - http://localhost:8080${NC}"
else
    echo -e "${RED}✗ App1 failed to start${NC}"
    FAILED=1
fi

if [ -n "$APP2_CHECK" ]; then
    echo -e "${GREEN}✓ App2 running (PID: $APP2_CHECK) - http://localhost:8081${NC}"
else
    echo -e "${RED}✗ App2 failed to start${NC}"
    FAILED=1
fi

if [ -n "$APP3_CHECK" ]; then
    echo -e "${GREEN}✓ App3 running (PID: $APP3_CHECK) - http://localhost:8082${NC}"
else
    echo -e "${RED}✗ App3 failed to start${NC}"
    FAILED=1
fi

if [ -n "$APP4_CHECK" ]; then
    echo -e "${GREEN}✓ App4 running (PID: $APP4_CHECK) - http://localhost:8083${NC}"
else
    echo -e "${RED}✗ App4 failed to start${NC}"
    FAILED=1
fi

echo "=========================================="
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All applications started successfully!${NC}"
    echo ""
    echo "Load generation has begun. Check:"
    echo "  • New Relic UI for APM data (Apps 1, 2, 3)"
    echo "  • Database connections: ps aux | grep dotnet"
    echo "  • Application logs in each app directory"
else
    echo -e "${RED}⚠ Some applications failed to start${NC}"
    echo "Check logs in each application directory for errors"
fi

echo ""
echo "Useful Commands:"
echo "  Status:        ./runAllApps.sh --status"
echo "  Stop apps:     ./runAllApps.sh --stop"
echo "  Monitor CPU:   watch -n 2 'ps aux | head -1 && ps aux | grep -E \"dotnet.*App[1-4]\" | grep -v grep'"
echo "  Check ports:   lsof -i :8080-8083"
echo ""
echo "Note: Logs are redirected to /dev/null to save disk space"
echo "      Monitor via New Relic UI for transaction data"
echo ""
echo "=========================================="
