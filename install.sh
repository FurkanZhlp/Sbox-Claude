#!/usr/bin/env bash
#
# s&box Claude Bridge Installer (Linux/WSL/Mac)
#
# 1. Detects your s&box install and copies the Bridge addon into <sbox>/addons
# 2. With a project path, mounts the addon into <project>/Libraries so it loads
#    with no .sbproj edit
# 3. Prompts (or accepts --client) for which MCP-compatible AI clients to
#    register the sbox server with: Claude Code, Codex, Cursor, Continue,
#    Claude Desktop
#
# Usage:
#   ./install.sh [SBOX_PATH] [PROJECT_PATH] [--client claude,codex,...] [--no-prompt]
#
# Examples:
#   ./install.sh
#   ./install.sh /path/to/sbox
#   ./install.sh /path/to/sbox /path/to/my-game
#   ./install.sh --client claude,codex
#   ./install.sh /path/to/sbox /path/to/my-game --client all
#   ./install.sh --no-prompt          # install addon only, no client config

set -euo pipefail

ADDON_NAME="sbox-bridge-addon"
PACKAGE_IDENT="sboxskinsgg.claudebridge"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ADDON_SOURCE="$SCRIPT_DIR/$ADDON_NAME"
MCP_COMMAND="npx"
MCP_ARG="sbox-mcp-server"

# ── Parse args ─────────────────────────────────────────────────────

SBOX_PATH_ARG=""
PROJECT_PATH_ARG=""
CLIENT_ARG=""
NO_PROMPT=0

POSITIONALS=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --client) CLIENT_ARG="$2"; shift 2 ;;
        --client=*) CLIENT_ARG="${1#*=}"; shift ;;
        --no-prompt) NO_PROMPT=1; shift ;;
        --help|-h)
            sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
            exit 0 ;;
        *) POSITIONALS+=( "$1" ); shift ;;
    esac
done

[[ ${#POSITIONALS[@]} -ge 1 ]] && SBOX_PATH_ARG="${POSITIONALS[0]}"
[[ ${#POSITIONALS[@]} -ge 2 ]] && PROJECT_PATH_ARG="${POSITIONALS[1]}"

PROJECT_PATH="${PROJECT_PATH_ARG:-${PROJECT_PATH:-}}"

echo ""
echo "=== s&box Claude Bridge Installer ==="
echo ""

# ── Locate s&box ───────────────────────────────────────────────────

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
    [[ -f "$steam_config" ]] || steam_config="$HOME/.local/share/Steam/steamapps/libraryfolders.vdf"
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

SBOX_PATH=$(find_sbox "$SBOX_PATH_ARG" 2>/dev/null) || {
    echo "ERROR: Could not auto-detect s&box. Run with the path:" >&2
    echo "  ./install.sh /path/to/sbox" >&2
    exit 1
}
echo "Found s&box at: $SBOX_PATH"

# ── Verify source ─────────────────────────────────────────────────

if [[ ! -d "$ADDON_SOURCE" ]]; then
    echo "ERROR: Cannot find $ADDON_NAME folder at $ADDON_SOURCE" >&2
    exit 1
fi
SRC_SBPROJ=$(find "$ADDON_SOURCE" -maxdepth 1 -type f -name "*.sbproj" | head -1)
if [[ -z "$SRC_SBPROJ" ]]; then
    echo "ERROR: Source addon at $ADDON_SOURCE has no .sbproj." >&2
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
cp -r "$ADDON_SOURCE" "$DESTINATION"
echo "  -> $DESTINATION"

# ── Optional: mount into project ──────────────────────────────────

if [[ -n "$PROJECT_PATH" ]]; then
    if [[ ! -d "$PROJECT_PATH" ]]; then
        echo "ERROR: ProjectPath not found: $PROJECT_PATH" >&2; exit 1
    fi
    PROJ_SBPROJ=$(find "$PROJECT_PATH" -maxdepth 1 -type f -name "*.sbproj" | head -1)
    if [[ -z "$PROJ_SBPROJ" ]]; then
        echo "ERROR: ProjectPath has no .sbproj." >&2; exit 1
    fi
    LIB_DIR="$PROJECT_PATH/Libraries"
    [[ -d "$LIB_DIR" ]] || mkdir -p "$LIB_DIR"
    LIB_DEST="$LIB_DIR/$PACKAGE_IDENT"
    [[ -d "$LIB_DEST" ]] && rm -rf "$LIB_DEST"
    cp -r "$ADDON_SOURCE" "$LIB_DEST"
    echo "Mounted into project: $LIB_DEST"
fi

# ── Determine clients ─────────────────────────────────────────────

VALID_CLIENTS=( claude codex cursor continue desktop )

parse_clients() {
    local raw="$1"
    raw=$(echo "$raw" | tr 'A-Z' 'a-z' | tr ',' ' ')
    if [[ -z "$raw" ]] || [[ "$raw" == "none" ]]; then echo ""; return; fi
    if [[ "$raw" == "all" ]]; then echo "${VALID_CLIENTS[*]}"; return; fi

    local picked=()
    for token in $raw; do
        local found=0
        for valid in "${VALID_CLIENTS[@]}"; do
            if [[ "$token" == "$valid" ]]; then picked+=( "$token" ); found=1; break; fi
        done
        [[ $found -eq 0 ]] && echo "  (ignoring unknown client '$token')" >&2
    done
    echo "${picked[*]}"
}

if [[ -n "$CLIENT_ARG" ]]; then
    CLIENTS_RAW="$CLIENT_ARG"
elif [[ $NO_PROMPT -eq 1 ]]; then
    CLIENTS_RAW=""
else
    echo ""
    echo "Which AI client(s) do you want to register the sbox MCP server with?"
    echo "  1) Claude Code         (~/.claude.json)"
    echo "  2) OpenAI Codex CLI    (~/.codex/config.toml)"
    echo "  3) Cursor              (~/.cursor/mcp.json)"
    echo "  4) Continue.dev        (~/.continue/config.json)"
    echo "  5) Claude Desktop      (~/Library/.../claude_desktop_config.json)"
    echo ""
    read -p "  Comma-separated names (claude,codex,...) or 'all' / 'none' [none]: " CLIENTS_RAW || CLIENTS_RAW=""
fi

CLIENTS=$(parse_clients "$CLIENTS_RAW")

# ── Helpers for client configuration ──────────────────────────────

write_mcp_json() {
    local path="$1"
    local dir; dir=$(dirname "$path")
    [[ -d "$dir" ]] || mkdir -p "$dir"

    if command -v jq >/dev/null 2>&1; then
        if [[ -f "$path" ]]; then
            tmp=$(mktemp)
            jq --arg cmd "$MCP_COMMAND" --arg arg "$MCP_ARG" \
                '.mcpServers.sbox = { command: $cmd, args: [$arg] }' \
                "$path" > "$tmp" && mv "$tmp" "$path"
        else
            jq -n --arg cmd "$MCP_COMMAND" --arg arg "$MCP_ARG" \
                '{ mcpServers: { sbox: { command: $cmd, args: [$arg] } } }' \
                > "$path"
        fi
        echo "  Wrote $path"
    else
        if [[ -f "$path" ]] && grep -q '"sbox"' "$path"; then
            echo "  Already present (jq not installed; manual merge skipped) -> $path"
            return
        fi
        cat > "$path" <<-EOF
		{
		  "mcpServers": {
		    "sbox": {
		      "command": "$MCP_COMMAND",
		      "args": ["$MCP_ARG"]
		    }
		  }
		}
		EOF
        echo "  Wrote $path (jq not available — overwrote)"
    fi
}

configure_claude() {
    if ! command -v claude >/dev/null 2>&1; then
        echo "  claude CLI not in PATH. After installing Claude Code, run:"
        echo "    claude mcp add sbox -- npx sbox-mcp-server"
        return
    fi
    echo "  Running: claude mcp add sbox -- npx sbox-mcp-server"
    if claude mcp add sbox -- npx sbox-mcp-server >/dev/null; then
        echo "  OK"
    else
        echo "  claude mcp add failed (already present?)"
    fi
}

configure_codex() {
    local path="$HOME/.codex/config.toml"
    local dir; dir=$(dirname "$path")
    [[ -d "$dir" ]] || mkdir -p "$dir"

    if [[ -f "$path" ]] && grep -qE '^\s*\[mcp_servers\.sbox\]' "$path"; then
        echo "  Already present in $path (skipping)"
        return
    fi

    cat >> "$path" <<-EOF

	[mcp_servers.sbox]
	command = "$MCP_COMMAND"
	args = ["$MCP_ARG"]
	EOF
    echo "  Appended [mcp_servers.sbox] to $path"
}

configure_cursor() {
    write_mcp_json "$HOME/.cursor/mcp.json"
}

configure_continue() {
    local path="$HOME/.continue/config.json"
    local dir; dir=$(dirname "$path")
    [[ -d "$dir" ]] || mkdir -p "$dir"

    if command -v jq >/dev/null 2>&1; then
        local block
        block=$(jq -n --arg cmd "$MCP_COMMAND" --arg arg "$MCP_ARG" \
            '{ transport: { type: "stdio", command: $cmd, args: [$arg] } }')

        if [[ -f "$path" ]]; then
            if grep -q "sbox-mcp-server" "$path"; then
                echo "  Already present in $path (skipping)"
                return
            fi
            tmp=$(mktemp)
            jq --argjson b "$block" \
                '.experimental.modelContextProtocolServers = ((.experimental.modelContextProtocolServers // []) + [$b])' \
                "$path" > "$tmp" && mv "$tmp" "$path"
        else
            jq -n --argjson b "$block" \
                '{ experimental: { modelContextProtocolServers: [$b] } }' \
                > "$path"
        fi
        echo "  Wrote $path"
    else
        echo "  jq not installed — please add this block to $path manually:"
        cat <<-EOF
		    "experimental": {
		      "modelContextProtocolServers": [
		        { "transport": { "type": "stdio", "command": "$MCP_COMMAND", "args": ["$MCP_ARG"] } }
		      ]
		    }
		EOF
    fi
}

configure_desktop() {
    local path
    if [[ "$(uname)" == "Darwin" ]]; then
        path="$HOME/Library/Application Support/Claude/claude_desktop_config.json"
    elif [[ -d "/mnt/c" ]] && [[ -n "${USERPROFILE:-}" ]]; then
        # WSL with Windows AppData reachable
        path="$(wslpath "$USERPROFILE")/AppData/Roaming/Claude/claude_desktop_config.json"
    else
        path="$HOME/.config/Claude/claude_desktop_config.json"
    fi
    write_mcp_json "$path"
}

# ── Run configurations ────────────────────────────────────────────

if [[ -n "$CLIENTS" ]]; then
    echo ""
    echo "Configuring AI clients..."
    for c in $CLIENTS; do
        echo "  [$c]"
        case "$c" in
            claude)   configure_claude ;;
            codex)    configure_codex ;;
            cursor)   configure_cursor ;;
            continue) configure_continue ;;
            desktop)  configure_desktop ;;
        esac
    done
fi

# ── Done ──────────────────────────────────────────────────────────

echo ""
echo "Installation complete."
echo ""
echo "Next steps:"
if [[ -n "$PROJECT_PATH" ]]; then
    echo "  - Open the project at $PROJECT_PATH in s&box"
else
    echo "  - In s&box, mount the addon (use ProjectPath next time, Project menu, or .sbproj edit)"
fi
echo "  - Restart s&box; the Bridge auto-starts on first frame"
if [[ -z "$CLIENTS" ]]; then
    echo "  - Configure an AI client later: re-run with --client claude,codex,..."
else
    echo "  - Restart your AI client(s) to pick up the new MCP server"
fi
echo ""
