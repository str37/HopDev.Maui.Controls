#!/usr/bin/env bash
set -euo pipefail

# ═══════════════════════════════════════════════════════════════
#  pack-local.sh — Pack HopDev.Maui.Controls to local NuGet
#
#  Usage:  ./pack-local.sh 1.0.0-local.1
#          ./pack-local.sh 1.0.0          (release candidate)
# ═══════════════════════════════════════════════════════════════

if [ -z "${1:-}" ]; then
    echo "Usage: ./pack-local.sh <version>"
    echo "  Example: ./pack-local.sh 1.0.0-local.1"
    exit 1
fi

VERSION="$1"
LOCAL_FEED="${LOCAL_NUGET_FEED:-$HOME/.local/share/LocalNuGet}"
ARTIFACTS="artifacts/packages"

echo ""
echo "═══════════════════════════════════════════════════════════"
echo " Packing HopDev.Maui.Controls v$VERSION"
echo " Target: $LOCAL_FEED"
echo "═══════════════════════════════════════════════════════════"
echo ""

mkdir -p "$LOCAL_FEED" "$ARTIFACTS"

# Clean
echo "[1/4] Cleaning..."
dotnet clean src/HopDev.Maui.Controls/HopDev.Maui.Controls.csproj -c Release --verbosity quiet 2>/dev/null || true

# Build
echo "[2/4] Building Release..."
dotnet build src/HopDev.Maui.Controls/HopDev.Maui.Controls.csproj -c Release -p:Version="$VERSION" --verbosity quiet

# Pack
echo "[3/4] Packing..."
dotnet pack src/HopDev.Maui.Controls/HopDev.Maui.Controls.csproj -c Release -p:Version="$VERSION" --output "$ARTIFACTS" --verbosity quiet

# Copy to local feed
echo "[4/4] Copying to $LOCAL_FEED..."
cp -f "$ARTIFACTS"/HopDev.Maui.Controls."$VERSION".nupkg "$LOCAL_FEED/"
cp -f "$ARTIFACTS"/HopDev.Maui.Controls."$VERSION".snupkg "$LOCAL_FEED/" 2>/dev/null || true

# Clear NuGet cache
echo "Clearing NuGet cache for HopDev.Maui.Controls..."
rm -rf ~/.nuget/packages/hopdev.maui.controls* 2>/dev/null || true

echo ""
echo "═══════════════════════════════════════════════════════════"
echo " SUCCESS: HopDev.Maui.Controls v$VERSION"
echo " Location: $LOCAL_FEED"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "Next steps:"
echo "  1. In consumer app, ensure nuget.config has LocalNuGet source"
echo "  2. Update PackageReference version to $VERSION"
echo "  3. dotnet restore --force"
echo ""
