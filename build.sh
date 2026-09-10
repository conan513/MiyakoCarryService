#!/usr/bin/env bash
set -euo pipefail

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ANSI Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo "======================================================="
echo "    MiyakoCarryService - Linux Release Build Script"
echo "======================================================="
echo ""

if [ ! -f "version.txt" ]; then
    echo -e "${RED}[ERROR] A version.txt fájl nem található!${NC}"
    exit 1
fi

VER="$(head -n 1 version.txt | tr -d '\r\n')"
echo -e "${BLUE}Verzió:${NC} ${VER}"
echo ""

# 1. Clean
echo -e "${BLUE}[1/3] Tisztítás (Clean - Release)...${NC}"
dotnet clean MiyakoCarryService.slnx -c Release
echo ""

# 2. Build
echo -e "${BLUE}[2/3] Fordítás (Build - Release: Client, Fika, Assistant, Server)...${NC}"
dotnet build MiyakoCarryService.slnx -c Release
echo ""

# 3. Packaging
echo -e "${BLUE}[3/3] Csomagolás (Zips létrehozása: Plugin, Fika, Assistant)...${NC}"

create_zip() {
    local src_dir="$1"
    local dest_zip="$2"
    if command -v zip &> /dev/null; then
        (cd "$src_dir" && zip -r -q "$dest_zip" .)
    elif command -v 7z &> /dev/null; then
        (cd "$src_dir" && 7z a -tzip -bso0 -bsp0 "$dest_zip" .)
    else
        python3 -c "import shutil, sys; shutil.make_archive(sys.argv[2].removesuffix('.zip'), 'zip', sys.argv[1])" "$src_dir" "$dest_zip"
    fi
}

# Remove previous archives
rm -f "MiyakoCarryService-${VER}.zip" "MiyakoCarryServiceFika-${VER}.zip" "MiyakoCarryServiceAssistant-${VER}.zip"

# Create archives
if [ -d "Build/Plugin" ]; then
    create_zip "Build/Plugin" "${SCRIPT_DIR}/MiyakoCarryService-${VER}.zip"
    echo "  -> MiyakoCarryService-${VER}.zip elkészült"
else
    echo -e "${RED}[WARNING] Build/Plugin mappa nem található!${NC}"
fi

if [ -d "Build/Fika" ]; then
    create_zip "Build/Fika" "${SCRIPT_DIR}/MiyakoCarryServiceFika-${VER}.zip"
    echo "  -> MiyakoCarryServiceFika-${VER}.zip elkészült"
else
    echo -e "${RED}[WARNING] Build/Fika mappa nem található!${NC}"
fi

if [ -d "Build/Assistant" ]; then
    create_zip "Build/Assistant" "${SCRIPT_DIR}/MiyakoCarryServiceAssistant-${VER}.zip"
    echo "  -> MiyakoCarryServiceAssistant-${VER}.zip elkészült"
else
    echo -e "${RED}[WARNING] Build/Assistant mappa nem található!${NC}"
fi

echo ""
echo "======================================================="
echo -e "${GREEN}[SUCCESS] A Release build és csomagolás sikeresen befejeződött!${NC}"
echo "======================================================="
