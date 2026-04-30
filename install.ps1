<#
.SYNOPSIS
    Installs the s&box Claude Bridge addon and (optionally) configures
    one or more MCP-compatible AI clients to use it.

.DESCRIPTION
    1. Detects your s&box install and copies the Bridge addon into <sbox>/addons.
    2. If -ProjectPath is given, also mounts the addon directly into that
       project's Libraries folder so it loads with zero manual steps.
    3. Prompts (or accepts a -Client list) for AI clients to register the
       sbox MCP server with: Claude Code, OpenAI Codex CLI, Cursor,
       Continue.dev, Claude Desktop.

    Compatible with Windows PowerShell 5.1 and PowerShell 7+.

.PARAMETER SboxPath
    Path to your s&box install. Auto-detected if omitted.

.PARAMETER ProjectPath
    Path to an s&box project (folder with the .sbproj). When given, the
    Bridge addon is also copied to <ProjectPath>\Libraries\sboxskinsgg.claudebridge
    so the project mounts it on next open without editing PackageReferences.

.PARAMETER Client
    Comma-separated list of AI clients to register the MCP server with.
    Valid: claude, codex, cursor, continue, desktop, all, none.
    If omitted you'll be prompted interactively.

.EXAMPLE
    .\install.ps1
    # Auto-detects s&box, prompts for which AI client(s) to configure

.EXAMPLE
    .\install.ps1 -ProjectPath "C:\path\to\my-game" -Client claude,codex
    # Mounts into project and registers sbox in Claude Code + Codex non-interactively

.EXAMPLE
    .\install.ps1 -Client none
    # Just installs the addon, no client config (skips the prompt)
#>

param(
    [string]$SboxPath = "",
    [string]$ProjectPath = "",
    [string]$Client = ""
)

$ErrorActionPreference = "Stop"
$addonName     = "sbox-bridge-addon"
$packageIdent  = "sboxskinsgg.claudebridge"
$mcpCommand    = "npx"
$mcpArgs       = @( "sbox-mcp-server" )

Write-Host ""
Write-Host "=== s&box Claude Bridge Installer ===" -ForegroundColor Cyan
Write-Host ""

# ── Helpers ──────────────────────────────────────────────────────

function Write-Utf8NoBom([string]$path, [string]$content) {
    # PS 5.1's Set-Content -Encoding utf8 writes a BOM that breaks several JSON
    # and TOML parsers. Use the .NET API with an explicit no-BOM encoding.
    [System.IO.File]::WriteAllText( $path, $content, (New-Object System.Text.UTF8Encoding $false) )
}

function Append-Utf8NoBom([string]$path, [string]$content) {
    if (Test-Path $path) {
        $existing = [System.IO.File]::ReadAllText( $path, (New-Object System.Text.UTF8Encoding $false) )
        Write-Utf8NoBom $path ($existing + $content)
    } else {
        Write-Utf8NoBom $path $content
    }
}

function Ensure-Dir([string]$dir) {
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

# ── Locate s&box installation ──────────────────────────────────────

function Find-SboxPath {
    $candidates = @(
        "$env:ProgramFiles\Steam\steamapps\common\sbox",
        "${env:ProgramFiles(x86)}\Steam\steamapps\common\sbox",
        "D:\SteamLibrary\steamapps\common\sbox",
        "E:\SteamLibrary\steamapps\common\sbox",
        "F:\SteamLibrary\steamapps\common\sbox",
        "$env:USERPROFILE\.sbox"
    )
    foreach ($path in $candidates) { if (Test-Path $path) { return $path } }

    $steamConfig = "${env:ProgramFiles(x86)}\Steam\steamapps\libraryfolders.vdf"
    if (Test-Path $steamConfig) {
        $content = Get-Content $steamConfig -Raw
        $vdfMatches = [regex]::Matches($content, '"path"\s+"([^"]+)"')
        foreach ($m in $vdfMatches) {
            $libPath = $m.Groups[1].Value -replace '\\\\', '\'
            $candidate = Join-Path $libPath "steamapps\common\sbox"
            if (Test-Path $candidate) { return $candidate }
        }
    }
    return $null
}

if ($SboxPath -eq "") {
    Write-Host "Searching for s&box installation..." -ForegroundColor Yellow
    $SboxPath = Find-SboxPath
    if ($null -eq $SboxPath) {
        Write-Host "Could not auto-detect s&box. Run with -SboxPath." -ForegroundColor Red
        exit 1
    }
}
if (-not (Test-Path $SboxPath)) {
    Write-Host "s&box path not found: $SboxPath" -ForegroundColor Red
    exit 1
}
Write-Host "Found s&box at: $SboxPath" -ForegroundColor Green

# ── Find addon source ─────────────────────────────────────────────

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$addonSource = Join-Path $scriptDir $addonName
if (-not (Test-Path $addonSource)) {
    $addonSource = Join-Path (Split-Path -Parent $scriptDir) $addonName
}
if (-not (Test-Path $addonSource)) {
    Write-Host "Cannot find $addonName folder. Run from the Sbox-Claude repo." -ForegroundColor Red
    exit 1
}
$srcSbproj = Get-ChildItem -Path $addonSource -Filter "*.sbproj" -File | Select-Object -First 1
if ($null -eq $srcSbproj) {
    Write-Host "Source addon at $addonSource has no .sbproj — aborting." -ForegroundColor Red
    exit 1
}

# ── Copy to global addons ─────────────────────────────────────────

$addonsDir = Join-Path $SboxPath "addons"
Ensure-Dir $addonsDir
$destination = Join-Path $addonsDir $addonName

if (Test-Path $destination) {
    Write-Host "Existing global install found. Replacing..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $destination
}
Write-Host "Copying Bridge addon to global addons..." -ForegroundColor Yellow
Copy-Item -Recurse -Force $addonSource $destination
Write-Host "  -> $destination" -ForegroundColor Gray

# ── Optional: mount into project ──────────────────────────────────

if ($ProjectPath -ne "") {
    if (-not (Test-Path $ProjectPath)) {
        Write-Host "ProjectPath not found: $ProjectPath" -ForegroundColor Red
        exit 1
    }
    $projSbproj = Get-ChildItem -Path $ProjectPath -Filter "*.sbproj" -File | Select-Object -First 1
    if ($null -eq $projSbproj) {
        Write-Host "ProjectPath has no .sbproj." -ForegroundColor Red
        exit 1
    }
    $libDir = Join-Path $ProjectPath "Libraries"
    Ensure-Dir $libDir
    $libDest = Join-Path $libDir $packageIdent
    if (Test-Path $libDest) { Remove-Item -Recurse -Force $libDest }
    Copy-Item -Recurse -Force $addonSource $libDest
    Write-Host "Mounted into project Libraries: $libDest" -ForegroundColor Green
}

# ── Determine which clients to configure ──────────────────────────

$validClients = @( "claude", "codex", "cursor", "continue", "desktop" )

function Parse-ClientChoice([string]$raw) {
    if ($null -eq $raw) { $raw = "" }
    $raw = $raw.ToLower().Trim()
    if ($raw -eq "" -or $raw -eq "none") { return @() }
    if ($raw -eq "all") { return $validClients }
    $parts = $raw -split '[,\s]+' | Where-Object { $_ -ne "" }
    $picked = @()
    foreach ($p in $parts) {
        if ($validClients -contains $p) { $picked += $p }
        else { Write-Host "  (ignoring unknown client '$p')" -ForegroundColor Yellow }
    }
    return $picked
}

if ($Client -ne "") {
    $clientsToConfigure = Parse-ClientChoice $Client
} else {
    Write-Host ""
    Write-Host "Which AI client(s) do you want to register the sbox MCP server with?" -ForegroundColor Cyan
    Write-Host "  1) Claude Code         (~/.claude.json - uses 'claude mcp add')"
    Write-Host "  2) OpenAI Codex CLI    (~/.codex/config.toml)"
    Write-Host "  3) Cursor              (~/.cursor/mcp.json)"
    Write-Host "  4) Continue.dev        (~/.continue/config.json)"
    Write-Host "  5) Claude Desktop      (%APPDATA%\Claude\claude_desktop_config.json)"
    Write-Host ""
    Write-Host "  Comma-separated names (claude,codex,...) or 'all' / 'none' [none]: " -ForegroundColor White -NoNewline
    $reply = Read-Host
    $clientsToConfigure = Parse-ClientChoice $reply
}

# ── JSON helpers (PS 5.1 + PS 7 compatible) ───────────────────────

function ConvertFrom-JsonSafe([string]$path) {
    if (-not (Test-Path $path)) { return $null }
    $raw = [System.IO.File]::ReadAllText( $path )
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    try { return $raw | ConvertFrom-Json }
    catch {
        Write-Host "  WARN: $path is not valid JSON - skipping." -ForegroundColor Yellow
        return "INVALID"
    }
}

function Add-OrSet-Property($obj, [string]$name, $value) {
    # Works on PSCustomObject (the type ConvertFrom-Json produces in PS 5.1).
    if ($null -eq $obj) { return }
    if ($obj.PSObject.Properties[$name]) {
        $obj.$name = $value
    } else {
        $obj | Add-Member -NotePropertyName $name -NotePropertyValue $value -Force
    }
}

function Set-McpJson([string]$path, [string]$serverName, [string]$command, [string[]]$serverArgs) {
    Ensure-Dir (Split-Path -Parent $path)

    $config = ConvertFrom-JsonSafe $path
    if ($config -eq "INVALID") { return $false }
    if ($null -eq $config) { $config = New-Object PSObject }

    # mcpServers
    if ($null -eq $config.mcpServers) {
        Add-OrSet-Property $config "mcpServers" (New-Object PSObject)
    }

    # mcpServers.<serverName>
    $entry = New-Object PSObject
    Add-OrSet-Property $entry "command" $command
    Add-OrSet-Property $entry "args" $serverArgs
    Add-OrSet-Property $config.mcpServers $serverName $entry

    Write-Utf8NoBom $path ($config | ConvertTo-Json -Depth 10)
    return $true
}

function Configure-Claude {
    if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
        Write-Host "  claude CLI not found in PATH - install Claude Code first, then run:" -ForegroundColor Yellow
        Write-Host "    claude mcp add sbox -- npx sbox-mcp-server" -ForegroundColor Gray
        return
    }
    Write-Host "  Running: claude mcp add sbox -- npx sbox-mcp-server"
    & claude mcp add sbox -- npx sbox-mcp-server | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Host "  OK" -ForegroundColor Green }
    else { Write-Host "  claude mcp add returned exit $LASTEXITCODE (already present?)" -ForegroundColor Yellow }
}

function Configure-Codex {
    $path = Join-Path $env:USERPROFILE ".codex\config.toml"
    Ensure-Dir (Split-Path -Parent $path)

    if (Test-Path $path) {
        $existing = [System.IO.File]::ReadAllText( $path )
        if ($existing -match "(?ms)^\s*\[mcp_servers\.sbox\]") {
            Write-Host "  Already present in $path (skipping)" -ForegroundColor Gray
            return
        }
    }

    $block = "`n[mcp_servers.sbox]`ncommand = `"npx`"`nargs = [`"sbox-mcp-server`"]`n"
    Append-Utf8NoBom $path $block
    Write-Host "  Appended [mcp_servers.sbox] to $path" -ForegroundColor Green
}

function Configure-Cursor {
    $path = Join-Path $env:USERPROFILE ".cursor\mcp.json"
    if (Set-McpJson $path "sbox" $mcpCommand $mcpArgs) {
        Write-Host "  Wrote $path" -ForegroundColor Green
    }
}

function Configure-Continue {
    $path = Join-Path $env:USERPROFILE ".continue\config.json"
    Ensure-Dir (Split-Path -Parent $path)

    $config = ConvertFrom-JsonSafe $path
    if ($config -eq "INVALID") { return }
    if ($null -eq $config) { $config = New-Object PSObject }

    if ($null -eq $config.experimental) {
        Add-OrSet-Property $config "experimental" (New-Object PSObject)
    }
    if ($null -eq $config.experimental.modelContextProtocolServers) {
        Add-OrSet-Property $config.experimental "modelContextProtocolServers" @()
    }

    $existingServers = @($config.experimental.modelContextProtocolServers)
    foreach ($srv in $existingServers) {
        if ($srv.transport -and $srv.transport.args -and ($srv.transport.args -contains "sbox-mcp-server")) {
            Write-Host "  Already present in $path (skipping)" -ForegroundColor Gray
            return
        }
    }

    $transport = New-Object PSObject
    Add-OrSet-Property $transport "type"    "stdio"
    Add-OrSet-Property $transport "command" $mcpCommand
    Add-OrSet-Property $transport "args"    $mcpArgs

    $serverEntry = New-Object PSObject
    Add-OrSet-Property $serverEntry "transport" $transport

    $newServers = $existingServers + $serverEntry
    Add-OrSet-Property $config.experimental "modelContextProtocolServers" $newServers

    Write-Utf8NoBom $path ($config | ConvertTo-Json -Depth 10)
    Write-Host "  Wrote $path" -ForegroundColor Green
}

function Configure-Desktop {
    $path = Join-Path $env:APPDATA "Claude\claude_desktop_config.json"
    if (Set-McpJson $path "sbox" $mcpCommand $mcpArgs) {
        Write-Host "  Wrote $path" -ForegroundColor Green
    }
}

# ── Run configurations ────────────────────────────────────────────

if ($clientsToConfigure.Count -gt 0) {
    Write-Host ""
    Write-Host "Configuring AI clients..." -ForegroundColor Cyan
    foreach ($c in $clientsToConfigure) {
        Write-Host "  [$c]" -ForegroundColor White
        switch ($c) {
            "claude"   { Configure-Claude }
            "codex"    { Configure-Codex }
            "cursor"   { Configure-Cursor }
            "continue" { Configure-Continue }
            "desktop"  { Configure-Desktop }
        }
    }
}

# ── Done ──────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Installation complete." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
if ($ProjectPath -ne "") {
    Write-Host "  - Open the project at $ProjectPath in s&box" -ForegroundColor White
} else {
    Write-Host "  - In s&box, mount the addon (-ProjectPath, Project menu, or .sbproj edit)" -ForegroundColor White
}
Write-Host "  - Restart s&box; the Bridge auto-starts on first frame" -ForegroundColor White
if ($clientsToConfigure.Count -eq 0) {
    Write-Host "  - Configure an AI client later: re-run with -Client claude,codex,..." -ForegroundColor White
} else {
    Write-Host "  - Restart your AI client(s) to pick up the new MCP server" -ForegroundColor White
}
Write-Host ""
