#!/bin/bash

# Load environment variables from .env file
if [ -f .env ]; then
    echo "Loading environment variables from .env file..."
    export $(cat .env | grep -v '^#' | xargs)
else
    echo "WARNING: .env file not found. Using default values."
    echo "Copy .env.example to .env and configure it."
fi

# Display configuration
echo "=========================================="
echo "App4 - Analytics Load Generator (NO APM)"
echo "=========================================="
echo "Database: ${DB_USERNAME}@${DB_HOST}:${DB_PORT}/${DB_SERVICE_NAME}"
echo "API Port: ${API_PORT:-8083}"
echo "Threads: ${THREADS:-20}"
echo "Pool Max: ${DB_POOL_MAX:-50}"
echo "New Relic: DISABLED (No APM)"
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
echo "Starting App4 Analytics Load Generator (No APM)..."
echo "Press Ctrl+C to stop"
echo ""

# Redirect logs to file if LOG_FILE is set, otherwise to console
if [ -n "$LOG_FILE" ]; then
    echo "Logs will be written to: $LOG_FILE"
    dotnet run 2>&1 | tee -a "$LOG_FILE"
else
    dotnet run
fi
