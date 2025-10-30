#!/bin/bash
# BeepDM CLI Build and Test Script (Linux/macOS)
# This script builds, packages, and optionally installs the BeepDM CLI tool

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
INSTALL=false
CLEAN=false
TEST=false
CONFIGURATION="Release"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --install)
            INSTALL=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --test)
            TEST=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--install] [--clean] [--test] [--configuration <Debug|Release>]"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║      BeepDM CLI - Build & Install Script      ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════╝${NC}"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo -e "${YELLOW}🧹 Cleaning previous builds...${NC}"
    
    rm -rf bin obj nupkg
    
    echo -e "${GREEN}✓ Clean complete${NC}"
    echo ""
fi

# Restore dependencies
echo -e "${YELLOW}📦 Restoring dependencies...${NC}"
dotnet restore
echo -e "${GREEN}✓ Restore complete${NC}"
echo ""

# Build project
echo -e "${YELLOW}🔨 Building CLI (Configuration: $CONFIGURATION)...${NC}"
dotnet build --configuration "$CONFIGURATION" --no-restore
echo -e "${GREEN}✓ Build complete${NC}"
echo ""

# Run tests if requested
if [ "$TEST" = true ]; then
    echo -e "${YELLOW}🧪 Running tests...${NC}"
    # Add test commands here when tests are available
    echo -e "${CYAN}ℹ No tests configured yet${NC}"
    echo ""
fi

# Pack the tool
echo -e "${YELLOW}📦 Creating NuGet package...${NC}"
dotnet pack --configuration "$CONFIGURATION" --no-build --output ./nupkg
echo -e "${GREEN}✓ Package created${NC}"
echo ""

# Install if requested
if [ "$INSTALL" = true ]; then
    echo -e "${YELLOW}🚀 Installing CLI tool...${NC}"
    
    # Check if tool is already installed
    if dotnet tool list --global | grep -q "thetechidea.beep.cli"; then
        echo -e "${CYAN}  Uninstalling existing version...${NC}"
        dotnet tool uninstall --global TheTechIdea.Beep.CLI || true
    fi
    
    echo -e "${CYAN}  Installing new version...${NC}"
    dotnet tool install --global --add-source ./nupkg TheTechIdea.Beep.CLI
    
    echo -e "${GREEN}✓ Installation complete${NC}"
    echo ""
    
    # Verify installation
    echo -e "${YELLOW}🔍 Verifying installation...${NC}"
    if command -v beep &> /dev/null; then
        VERSION=$(beep --version 2>&1 || echo "unknown")
        echo -e "${GREEN}✓ CLI is installed and working!${NC}"
        echo -e "${CYAN}  Version: $VERSION${NC}"
    else
        echo -e "${YELLOW}⚠ Installation succeeded but verification failed${NC}"
        echo -e "${CYAN}  You may need to add ~/.dotnet/tools to your PATH${NC}"
        echo -e "${CYAN}  export PATH=\"\$PATH:\$HOME/.dotnet/tools\"${NC}"
    fi
    echo ""
fi

# Summary
echo -e "${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║              Build Summary                     ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${CYAN}Configuration:  $CONFIGURATION${NC}"
PACKAGE=$(ls -1 ./nupkg/*.nupkg 2>/dev/null | head -1 | xargs basename)
echo -e "${CYAN}Package:        $PACKAGE${NC}"
if [ "$INSTALL" = true ]; then
    echo -e "${CYAN}Installed:      Yes ✓${NC}"
else
    echo -e "${CYAN}Installed:      No (use --install flag)${NC}"
fi
echo ""

if [ "$INSTALL" = false ]; then
    echo -e "${YELLOW}💡 To install the CLI globally, run:${NC}"
    echo -e "${NC}   ./build-and-test.sh --install${NC}"
    echo ""
fi

echo -e "${YELLOW}📚 Next steps:${NC}"
echo -e "${NC}   • Run 'beep --help' to see available commands${NC}"
echo -e "${NC}   • Read QUICKSTART.md for getting started${NC}"
echo -e "${NC}   • Read FEATURES.md for complete feature list${NC}"
echo ""

echo -e "${GREEN}✅ All done! Happy coding! 🚀${NC}"

