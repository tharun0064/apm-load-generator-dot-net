#!/bin/bash

# Build script for all load generator applications

set -e  # Exit on error

echo "=========================================="
echo "Building Oracle Load Generator Applications (.NET)"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: .NET SDK is not installed or not in PATH${NC}"
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version | cut -d'.' -f1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo -e "${RED}ERROR: .NET 8.0 or higher is required${NC}"
    echo "Current .NET version: $(dotnet --version)"
    exit 1
fi

echo ".NET SDK version:"
dotnet --version
echo ""

# Build App1
echo "Building Application 1: OLTP Load Generator"
echo "--------------------------------------------"
cd App1OltpLoadGenerator
dotnet restore
dotnet build --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ OLTP Load Generator built successfully${NC}"
    echo "  Output: App1OltpLoadGenerator/bin/Release/net8.0/"
else
    echo -e "${RED}✗ OLTP Load Generator build failed${NC}"
    exit 1
fi
cd ..

echo ""
echo "Building Application 2: Analytics Load Generator"
echo "------------------------------------------------"
cd App2AnalyticsLoadGenerator
dotnet restore
dotnet build --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Analytics Load Generator built successfully${NC}"
    echo "  Output: App2AnalyticsLoadGenerator/bin/Release/net8.0/"
else
    echo -e "${RED}✗ Analytics Load Generator build failed${NC}"
    exit 1
fi
cd ..

echo ""
echo "Building Application 3: Analytics Load Generator 3"
echo "------------------------------------------------"
cd App3AnalyticsLoadGenerator
dotnet restore
dotnet build --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Analytics Load Generator 3 built successfully${NC}"
    echo "  Output: App3AnalyticsLoadGenerator/bin/Release/net8.0/"
else
    echo -e "${RED}✗ Analytics Load Generator 3 build failed${NC}"
    exit 1
fi
cd ..

echo ""
echo "Building Application 4: Analytics Load Generator (No APM)"
echo "--------------------------------------------------------"
cd App4AnalyticsLoadGeneratorNoApm
dotnet restore
dotnet build --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Analytics Load Generator (No APM) built successfully${NC}"
    echo "  Output: App4AnalyticsLoadGeneratorNoApm/bin/Release/net8.0/"
else
    echo -e "${RED}✗ Analytics Load Generator (No APM) build failed${NC}"
    exit 1
fi
cd ..

echo ""
echo "=========================================="
echo "Build Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Set up Oracle database (see oracle-setup.sql)"
echo ""
echo "   sqlplus sys as sysdba @oracle-setup.sql"
echo ""
echo "2. Configure database connection for each app:"
echo ""
echo "   cd App1OltpLoadGenerator && cp .env.example .env"
echo "   cd ../App2AnalyticsLoadGenerator && cp .env.example .env"
echo "   cd ../App3AnalyticsLoadGenerator && cp .env.example .env"
echo "   cd ../App4AnalyticsLoadGeneratorNoApm && cp .env.example .env"
echo ""
echo "   Then edit each .env file with your credentials"
echo ""
echo "3. Run the applications:"
echo ""
echo "   # Run all apps together:"
echo "   ./runAllApps.sh"
echo ""
echo "   # Or run individually:"
echo "   cd App1OltpLoadGenerator && ./run.sh"
echo "   cd App2AnalyticsLoadGenerator && ./run.sh"
echo "   cd App3AnalyticsLoadGenerator && ./run.sh"
echo "   cd App4AnalyticsLoadGeneratorNoApm && ./run.sh"
echo ""
echo "See README.md for complete documentation"
echo ""
