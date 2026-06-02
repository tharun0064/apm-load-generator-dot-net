#!/bin/bash

# Load environment variables from .env file
if [ -f .env ]; then
    echo "Loading environment variables from .env file..."
    export $(cat .env | grep -v '^#' | xargs)
else
    echo "WARNING: .env file not found. Using default values."
    echo "Copy .env.example to .env and configure it."
fi

# Set New Relic environment variables
export NEW_RELIC_LICENSE_KEY="${NEW_RELIC_LICENSE_KEY}"
export NEW_RELIC_APP_NAME="${NEW_RELIC_APP_NAME:-App1-OLTP-LoadGenerator-DotNet}"

# Display configuration
echo "=========================================="
echo "App1 - OLTP Load Generator"
echo "=========================================="
echo "Database: ${DB_USERNAME}@${DB_HOST}:${DB_PORT}/${DB_SERVICE_NAME}"
echo "API Port: ${API_PORT:-8080}"
echo "Threads: ${THREADS:-50}"
echo "Pool Max: ${DB_POOL_MAX:-100}"
echo "New Relic App: ${NEW_RELIC_APP_NAME}"
echo "=========================================="
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed."
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Restore dependencies if needed
if [ ! -d "bin" ] || [ ! -d "obj" ]; then
    echo "Restoring dependencies..."
    dotnet restore
fi

# Run the application
echo "Starting App1 OLTP Load Generator..."
echo "Press Ctrl+C to stop"
echo ""

# Redirect logs to file if LOG_FILE is set, otherwise to console
if [ -n "$LOG_FILE" ]; then
    echo "Logs will be written to: $LOG_FILE"
    dotnet run 2>&1 | tee -a "$LOG_FILE"
else
    dotnet run
fi
