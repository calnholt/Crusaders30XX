#!/usr/bin/env bash
# One-time setup for compiling MonoGame .fx shaders on macOS/Linux (MGFXC via Wine).
# See: https://docs.monogame.net/articles/getting_started/tools/mgfxc.html

set -euo pipefail

WINE64="$(command -v wine64 2>/dev/null || true)"
if [ -z "$WINE64" ]; then
  WINE64="$(command -v wine 2>/dev/null || true)"
fi
if [ -z "$WINE64" ]; then
  echo "error: wine or wine64 not found."
  echo "Install Wine, e.g.: brew install --cask wine-stable"
  exit 1
fi

if ! command -v 7z >/dev/null 2>&1; then
  echo "error: 7z not found. Install p7zip, e.g.: brew install p7zip"
  exit 1
fi

WINE_VERSION="$("$WINE64" --version 2>&1 | sed -n 's/.*wine-\([0-9]*\).*/\1/p')"
if [ -z "$WINE_VERSION" ] || [ "$WINE_VERSION" -lt 8 ]; then
  echo "error: Wine 8.0+ is required (found: $("$WINE64" --version 2>&1))."
  exit 1
fi

export WINEARCH=win64
export WINEPREFIX="${WINEPREFIX:-$HOME/.winemonogame}"

echo "Using $WINE64 with WINEPREFIX=$WINEPREFIX"
"$WINE64" wineboot -u

TEMP_DIR="${TMPDIR:-/tmp}/winemg2-$$"
mkdir -p "$TEMP_DIR"
trap 'rm -rf "$TEMP_DIR"' EXIT

# Disable Wine crash dialogs during unattended setup.
cat > "$TEMP_DIR/crashdialog.reg" <<'EOF'
REGEDIT4
[HKEY_CURRENT_USER\Software\Wine\WineDbg]
"ShowCrashDialog"=dword:00000000
EOF
"$WINE64" regedit "$TEMP_DIR/crashdialog.reg"

DOTNET_URL="https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.201/dotnet-sdk-8.0.201-win-x64.zip"
echo "Downloading Windows .NET SDK for MGFXC..."
curl -fsSL "$DOTNET_URL" -o "$TEMP_DIR/dotnet-sdk.zip"
echo "Extracting into Wine prefix..."
7z x "$TEMP_DIR/dotnet-sdk.zip" -o"$WINEPREFIX/drive_c/windows/system32/" -y >/dev/null

FIREFOX_URL="https://download-installer.cdn.mozilla.net/pub/firefox/releases/62.0.3/win64/ach/Firefox%20Setup%2062.0.3.exe"
echo "Installing d3dcompiler_47.dll..."
curl -fsSL "$FIREFOX_URL" -o "$TEMP_DIR/firefox.exe"
7z e "$TEMP_DIR/firefox.exe" "core/d3dcompiler_47.dll" -o"$WINEPREFIX/drive_c/windows/system32/" -aoa -y >/dev/null

if [ ! -f "$WINEPREFIX/drive_c/windows/system32/dotnet.exe" ]; then
  echo "error: dotnet.exe was not installed into the Wine prefix."
  exit 1
fi

echo ""
echo "MGFXC Wine prefix ready at: $WINEPREFIX"
echo "dotnet build will use MGFXC_WINE_PATH automatically (see Directory.Build.targets)."
echo ""
echo "Optional: add to your shell profile:"
echo "  export MGFXC_WINE_PATH=$WINEPREFIX"
