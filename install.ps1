<#
.SYNOPSIS
    Installs the s&box Claude Bridge addon.

.DESCRIPTION
    Detects your s&box installation, copies the Bridge addon into the
    addons directory, and (optionally) into a project's Libraries folder
    so it's mounted automatically without editing the .sbproj manually.

    After running this, restart s&box and the Bridge will start
    automatically — open Claude Code (or Codex / Cursor / etc.) and you
    have the bridge.

.PARAMETER SboxPath
    Path to your s&box install. Auto-detected if omitted.

.PARAMETER ProjectPath
    Optional path to an s&box project (folder containing the .sbproj).
    When given, the Bridge addon is also copied into
    "<ProjectPath>\Libraries\sboxskinsgg.claudebridge" so the project
    mounts it on next open without you editing PackageReferences.

.EXAMPLE
    .\install.ps1
    # Auto-detects s&box and installs to global addons

.EXAMPLE
    .\install.ps1 -ProjectPath "C:\path\to\my-game"
    # Installs globally AND mounts directly into a project

.EXAMPLE
    .\install.ps1 -SboxPath "D:\SteamLibrary\steamapps\common\sbox" -ProjectPath "C:\path\to\my-game"
#>

param(
    [string]$SboxPath = "",
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"
$addonName     = "sbox-bridge-addon"
$packageIdent  = "sboxskinsgg.claudebridge"

Write-Host ""
Write-Host "=== s&box Claude Bridge Installer ===" -ForegroundColor Cyan
Write-Host ""

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

    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }

    $steamConfig = "${env:ProgramFiles(x86)}\Steam\steamapps\libraryfolders.vdf"
    if (Test-Path $steamConfig) {
        $content = Get-Content $steamConfig -Raw
        $matches = [regex]::Matches($content, '"path"\s+"([^"]+)"')
        foreach ($match in $matches) {
            $libPath = $match.Groups[1].Value -replace '\\\\', '\'
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
        Write-Host "Could not auto-detect s&box installation." -ForegroundColor Red
        Write-Host ""
        Write-Host "Run with -SboxPath:" -ForegroundColor Yellow
        Write-Host '  .\install.ps1 -SboxPath "C:\path\to\sbox"' -ForegroundColor White
        exit 1
    }
}

if (-not (Test-Path $SboxPath)) {
    Write-Host "s&box path not found: $SboxPath" -ForegroundColor Red
    exit 1
}

Write-Host "Found s&box at: $SboxPath" -ForegroundColor Green

# ── Determine addons directory ─────────────────────────────────────

$addonsDir = Join-Path $SboxPath "addons"
if (-not (Test-Path $addonsDir)) {
    $altAddonsDir = Join-Path $env:USERPROFILE ".sbox\addons"
    if (Test-Path $altAddonsDir) {
        $addonsDir = $altAddonsDir
    } else {
        Write-Host "Creating addons directory: $addonsDir" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $addonsDir -Force | Out-Null
    }
}

Write-Host "Addons directory: $addonsDir" -ForegroundColor Green

# ── Find the addon source ─────────────────────────────────────────

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$addonSource = Join-Path $scriptDir $addonName

if (-not (Test-Path $addonSource)) {
    $addonSource = Join-Path (Split-Path -Parent $scriptDir) $addonName
}

if (-not (Test-Path $addonSource)) {
    Write-Host "Cannot find $addonName folder. Run from the Sbox-Claude repository." -ForegroundColor Red
    exit 1
}

# Verify the source has its sbproj (any .sbproj — addon folder name and
# package ident may differ; we don't hard-code the filename).
$srcSbproj = Get-ChildItem -Path $addonSource -Filter "*.sbproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $srcSbproj) {
    Write-Host "Source addon at $addonSource has no .sbproj — aborting." -ForegroundColor Red
    exit 1
}

# ── Copy to global addons (always) ────────────────────────────────

$destination = Join-Path $addonsDir $addonName

if (Test-Path $destination) {
    Write-Host "Existing global install found. Replacing..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $destination
}

Write-Host "Copying Bridge addon to global addons..." -ForegroundColor Yellow
Copy-Item -Recurse -Force $addonSource $destination

if (-not (Test-Path (Join-Path $destination $srcSbproj.Name))) {
    Write-Host "WARNING: Global copy seems incomplete — $($srcSbproj.Name) missing." -ForegroundColor Red
    exit 1
}

Write-Host "  -> $destination" -ForegroundColor Gray

# ── Optional: mount directly into a project ───────────────────────

if ($ProjectPath -ne "") {
    if (-not (Test-Path $ProjectPath)) {
        Write-Host "ProjectPath not found: $ProjectPath" -ForegroundColor Red
        exit 1
    }

    $projSbproj = Get-ChildItem -Path $ProjectPath -Filter "*.sbproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $projSbproj) {
        Write-Host "ProjectPath has no .sbproj — make sure it's an s&box project root." -ForegroundColor Red
        exit 1
    }

    $libDir = Join-Path $ProjectPath "Libraries"
    if (-not (Test-Path $libDir)) {
        New-Item -ItemType Directory -Path $libDir -Force | Out-Null
    }

    $libDest = Join-Path $libDir $packageIdent
    if (Test-Path $libDest) {
        Write-Host "Existing project mount found. Replacing..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force $libDest
    }

    Write-Host "Mounting Bridge into project Libraries..." -ForegroundColor Yellow
    Copy-Item -Recurse -Force $addonSource $libDest
    Write-Host "  -> $libDest" -ForegroundColor Gray
}

# ── Done ──────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Installation successful!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan

if ($ProjectPath -ne "") {
    Write-Host "  1. Open the project at $ProjectPath in s&box" -ForegroundColor White
    Write-Host "     (Bridge is already mounted via Libraries — no .sbproj edit needed)" -ForegroundColor Gray
} else {
    Write-Host "  1. Mount the addon in your project — pick one:" -ForegroundColor White
    Write-Host "     a. Re-run with -ProjectPath ""<your-project-folder>""" -ForegroundColor Gray
    Write-Host "     b. In the s&box editor: Project -> Add Package -> sboxskinsgg.claudebridge" -ForegroundColor Gray
    Write-Host "     c. Or add to .sbproj manually:" -ForegroundColor Gray
    Write-Host '         "PackageReferences": [ "sboxskinsgg.claudebridge" ]' -ForegroundColor DarkGray
}

Write-Host "  2. Restart s&box — the Bridge starts automatically on first frame" -ForegroundColor White
Write-Host "  3. Connect your AI client. Pick yours:" -ForegroundColor White
Write-Host "     - Claude Code:" -ForegroundColor Gray
Write-Host "         claude mcp add sbox -- npx sbox-mcp-server" -ForegroundColor Green
Write-Host "     - OpenAI Codex CLI: edit ~/.codex/config.toml" -ForegroundColor Gray
Write-Host "         [mcp_servers.sbox]" -ForegroundColor DarkGray
Write-Host '         command = "npx"' -ForegroundColor DarkGray
Write-Host '         args = ["sbox-mcp-server"]' -ForegroundColor DarkGray
Write-Host "     - Cursor / Continue / Claude Desktop: see INSTALL.md for snippets" -ForegroundColor Gray
Write-Host ""
