# Upload SE_Overclock_mod to Steam Workshop as "SE Overclock" (AppID 244850).
#
# Usage:
#   .\Deploy-Overclock.ps1
# Credentials are read from .env in the repo root (STEAM_USER, STEAM_PASS).
# Optional env overrides: STEAMCMD, STEAM_OVERCLOCK_PUBLISHED_ID
#   STEAM_OVERCLOCK_PUBLISHED_ID = Workshop file id to update. If unset, a non-zero "publishedfileid"
#     in se_overclock_workshop.vdf (left over from a previous run or hand-edited) is reused.
#     If still unknown, "0" creates a NEW item — then set the id (env or VDF) before the next run.

$ErrorActionPreference = "Stop"

# Load .env from repo root (two levels up from tools/steamcmd)
$envFile = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*?)\s*=\s*(.*?)\s*$') {
            $key = $matches[1]
            $val = $matches[2] -replace '^["'']|["'']$'
            if (-not [System.Environment]::GetEnvironmentVariable($key)) {
                [System.Environment]::SetEnvironmentVariable($key, $val, "Process")
            }
        }
    }
}

function Get-SteamLogin {
    $user = $env:STEAM_USER
    $pass = $env:STEAM_PASS
    if ([string]::IsNullOrWhiteSpace($user)) { Write-Error "STEAM_USER is not set. Add it to .env or set the environment variable." }
    if ([string]::IsNullOrWhiteSpace($pass)) { Write-Error "STEAM_PASS is not set. Add it to .env or set the environment variable." }
    return @{ User = $user; Pass = $pass }
}

$steamCmd = if ($env:STEAMCMD) { $env:STEAMCMD } else { "C:\SteamCMD\steamcmd.exe" }
if (-not (Test-Path $steamCmd)) {
    Write-Error "SteamCMD not found at $steamCmd. Set STEAMCMD to steamcmd.exe path."
}

$login = Get-SteamLogin
$user = $login.User
$pass = $login.Pass

$toolsDir = $PSScriptRoot
$repoRoot = Split-Path (Split-Path $toolsDir -Parent) -Parent
# Space Engineers Workshop preview must stay under ~1 MB (Steam rejects larger files).
$previewJpg = Join-Path $toolsDir "workshop_preview.jpg"
$previewPng = Join-Path $toolsDir "workshop_preview.png"
if (Test-Path $previewJpg) {
    $preview = $previewJpg
} elseif (Test-Path $previewPng) {
    $preview = $previewPng
} else {
    $preview = $previewPng
    $pngB64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
    [System.IO.File]::WriteAllBytes($preview, [Convert]::FromBase64String($pngB64))
}
$previewBytes = (Get-Item -LiteralPath $preview).Length
if ($previewBytes -gt 900000) {
    Write-Warning "Preview file is $previewBytes bytes; Space Engineers Workshop preview must be under 1 MB. Use workshop_preview.jpg (see repo) or shrink workshop_preview.png."
}

function Escape-VdfPath([string]$p) { return $p.Replace('\', '\\') }

function Get-PublishedFileIdFromVdf([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $raw = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    if ($raw -match '"publishedfileid"\s+"(\d+)"') {
        $id = $matches[1].Trim()
        if ($id -ne "0") { return $id }
    }
    return $null
}

$vdfPath = Join-Path $toolsDir "se_overclock_workshop.vdf"

$pubId = $null
if (-not [string]::IsNullOrWhiteSpace($env:STEAM_OVERCLOCK_PUBLISHED_ID)) {
    $pubId = $env:STEAM_OVERCLOCK_PUBLISHED_ID.Trim()
} else {
    $fromFile = Get-PublishedFileIdFromVdf -Path $vdfPath
    if ($null -ne $fromFile) {
        $pubId = $fromFile
        Write-Host "Using publishedfileid from existing VDF: $pubId (set STEAM_OVERCLOCK_PUBLISHED_ID to override)"
    }
}
if ([string]::IsNullOrWhiteSpace($pubId)) {
    $pubId = "0"
}

$prevEsc = Escape-VdfPath $preview
$contentEsc = Escape-VdfPath (Join-Path $repoRoot "SE_Overclock_mod")

$descFile = Join-Path $toolsDir "description.txt"
$description = if (Test-Path $descFile) {
    (Get-Content $descFile -Raw -Encoding UTF8).Replace('"', "'").TrimEnd()
} else {
    "SE Overclock — Upgrade module system for Space Engineers."
}

@"
"workshopitem"
{
	"appid"		"244850"
	"publishedfileid"	"$pubId"
	"contentfolder"		"$contentEsc"
	"previewfile"		"$prevEsc"
	"visibility"		"0"
	"title"			"SE Overclock"
	"description"		"$description"
	"changenote"		"SE Overclock — SteamCMD publish"
}
"@ | Set-Content -Path $vdfPath -Encoding ASCII

Write-Host "Workshop VDF: $vdfPath"
Write-Host "Content: $(Join-Path $repoRoot 'SE_Overclock_mod')"
if ($pubId -eq "0") {
    Write-Warning "publishedfileid is 0: Steam creates a NEW item. Note the FileID from output and set STEAM_OVERCLOCK_PUBLISHED_ID for next run."
}

& $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +workshop_build_item $vdfPath +quit

if ($LASTEXITCODE -ne 0) {
    Write-Error "SteamCMD exited with code $LASTEXITCODE"
}
Write-Host "Done."
