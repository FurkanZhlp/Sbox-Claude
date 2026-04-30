#!/usr/bin/env bash
#
# s&box Claude Bridge Installer (Linux/WSL/Mac)
#
# Detects your s&box installation via Steam, copies the Bridge addon
# into the addons directory, and optionally mounts it into a specific
# project so the .sbproj doesn't need to be edited manually.
#
# Usage:
#   ./install.sh                                          # Auto-detect s&box, global install only
#   ./install.sh /path/to/sbox                            # Explicit s&box path
#   ./install.sh /path/to/sbox /path/to/my-game           # Also mount into a project
#   SBOX_PATH=/path/to/sbox PROJECT_PATH=/path/to/game ./install.sh

set -euo pipefail

ADDON_NAME="sbox-bridge-addon"
PACKAGE_IDENT="sboxskinsgg.claudebridge"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ADDON_SOURCE="$SCRIPT_DIR/$ADDON_NAME"

echo ""
echo "=== s&box Claude Bridge Installer ==="
echo ""

# ── Locate s&box installation ──────────────────────────────────────

find_sbox() {
    if [[ -n "${1:-}" ]] && [[ -d "$1" ]]; then echo "$1"; return; fi
    if [[ -n "${SBOX_PATH:-}" ]] && [[ -d "$SBOX_PATH" ]]; then echo "$SBOX_PATH"; return; fi

    local candidates=(
        "$HOME/.steam/steam/steamapps/common/sbox"
        "$HOME/.local/share/Steam/steamapps/common/sbox"
    )

    if [[ -d "/mnt/c" ]]; then
        candidates+=(
            "/mnt/c/Program Files/Steam/steamapps/common/sbox"
            "/mnt/c/Program Files (x86)/Steam/steamapps/common/sbox"
            "/mnt/d/SteamLibrary/steamapps/common/sbox"
            "/mnt/e/SteamLibrary/steamapps/common/sbox"
        )
    fi

    candidates+=( "$HOME/Library/Application Support/Steam/steamapps/common/sbox" )

    for path in "${candidates[@]}"; do
        if [[ -d "$path" ]]; then echo "$path"; return; fi
    done

    local steam_config="$HOME/.steam/steam/steamapps/libraryfolders.vdf"
    if [[ ! -f "$steam_config" ]]; then
        steam_config="$HOME/.local/share/Steam/steamapps/libraryfolders.vdf"
    fi

    if [[ -f "$steam_config" ]]; then
        while IFS= read -r line; do
            local lib_path
            lib_path=$(echo "$line" | grep -oP '"path"\s+"\K[^"]+' 2>/dev/null || true)
            if [[ -n "$lib_path" ]] && [[ -d "$lib_path/steamapps/common/sbox" ]]; then
                echo "$lib_path/steamapps/common/sbox"
                return
            fi
        done < "$steam_config"
    fi

    return 1
}

SBOX_PATH=$(find_sbox "${1:-}" 2>/dev/null) || {
    echo "ERROR: Could not auto-detect s&box installation." >&2
    echo ""
    echo "Run with the path:"
    echo "  ./install.sh /path/to/sbox"
    echo ""
    echo "Common locations:"
    echo "  Linux:  ~/.steam/steam/steamapps/common/sbox"
    echo "  WSL:    /mnt/c/Program Files/Steam/steamapps/common/sbox"
    exit 1
}

PROJECT_PATH="${2:-${PROJECT_PATH:-}}"

echo "Found s&box at: $SBOX_PATH"

# ── Verify source ─────────────────────────────────────────────────

if [[ ! -d "$ADDON_SOURCE" ]]; then
    echo "ERROR: Cannot find $ADDON_NAME folder at $ADDON_SOURCE" >&2
    echo "Run from the Sbox-Claude repository root." >&2
    exit 1
fi

# Find any .sbproj in the source (filename may differ from folder name)
SRC_SBPROJ=$(find "$ADDON_SOURCE" -maxdepth 1 -type f -name "*.sbproj" | head -1)
if [[ -z "$SRC_SBPROJ" ]]; then
    echo "Source addon at $ADDON_SOURCE has no .sbproj — aborting." >&2
    exit 1
fi
SBPROJ_NAME=$(basename "$SRC_SBPROJ")

# ── Copy to global addons ─────────────────────────────────────────

ADDONS_DIR="$SBOX_PATH/addons"
[[ -d "$ADDONS_DIR" ]] || mkdir -p "$ADDONS_DIR"

DESTINATION="$ADDONS_DIR/$ADDON_NAME"

if [[ -d "$DESTINATION" ]]; then
    echo "Existing global install found. Replacing..."
    rm -rf "$DESTINATION"
fi

echo "Copying Bridge addon to global addons..."
cp -r "$ADDON_SOURCE" "$DESTINATION"

if [[ ! -f "$DESTINATION/$SBPROJ_NAME" ]]; then
    echo "WARNING: Global copy incomplete — $SBPROJ_NAME missing." >&2
    exit 1
fi

echo "  -> $DESTINATION"

# ── Optional: mount directly into a project ───────────────────────

if [[ -n "$PROJECT_PATH" ]]; then
    if [[ ! -d "$PROJECT_PATH" ]]; then
        echo "ERROR: ProjectPath not found: $PROJECT_PATH" >&2
        exit 1
    fi

    PROJ_SBPROJ=$(find "$PROJECT_PATH" -maxdepth 1 -type f -name "*.sbproj" | head -1)
    if [[ -z "$PROJ_SBPROJ" ]]; then
        echo "ERROR: ProjectPath has no .sbproj — make sure it's an s&box project root." >&2
        exit 1
    fi

    LIB_DIR="$PROJECT_PATH/Libraries"
    [[ -d "$LIB_DIR" ]] || mkdir -p "$LIB_DIR"

    LIB_DEST="$LIB_DIR/$PACKAGE_IDENT"
    if [[ -d "$LIB_DEST" ]]; then
        echo "Existing project mount found. Replacing..."
        rm -rf "$LIB_DEST"
    fi

    echo "Mounting Bridge into project Libraries..."
    cp -r "$ADDON_SOURCE" "$LIB_DEST"
    echo "  -> $LIB_DEST"
fi

# ── Done ──────────────────────────────────────────────────────────

echo ""
echo "Installation successful!"
echo ""
echo "Next steps:"

if [[ -n "$PROJECT_PATH" ]]; then
    echo "  1. Open the project at $PROJECT_PATH in s&box"
    echo "     (Bridge is already mounted via Libraries — no .sbproj edit needed)"
else
    echo "  1. Mount the addon in your project — pick one:"
    echo "     a. Re-run: ./install.sh \"$SBOX_PATH\" /path/to/your-project"
    echo "     b. In the s&box editor: Project -> Add Package -> $PACKAGE_IDENT"
    echo "     c. Or add to .sbproj manually:"
    echo "         \"PackageReferences\": [ \"$PACKAGE_IDENT\" ]"
fi

echo "  2. Restart s&box — the Bridge starts automatically on first frame"
echo "  3. Connect your AI client. Pick yours:"
echo "     - Claude Code:"
echo "         claude mcp add sbox -- npx sbox-mcp-server"
echo "     - OpenAI Codex CLI: edit ~/.codex/config.toml"
echo "         [mcp_servers.sbox]"
echo '         command = "npx"'
echo '         args = ["sbox-mcp-server"]'
echo "     - Cursor / Continue / Claude Desktop: see INSTALL.md for snippets"
echo ""
