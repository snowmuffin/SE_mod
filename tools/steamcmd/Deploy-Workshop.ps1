# Deploy both SE mods to Space Engineers Steam Workshop (AppID 244850).
# Requires: SteamCMD, Space Engineers on the account, and valid credentials.
#
# Usage (PowerShell):
#   $env:STEAM_USER = "your_steam_login"
#   $env:STEAM_PASS = "your_password_or_steam_guard_aware_app_password"
#   .\Deploy-Workshop.ps1
#
# Optional:
#   $env:STEAMCMD = "D:\SteamCMD\steamcmd.exe"   # default: C:\SteamCMD\steamcmd.exe
#   $env:STEAM_PRIME_PUBLISHED_ID = "0"          # default 0 = create NEW Prime item; set to existing id to update

$ErrorActionPreference = "Stop"

$steamCmd = if ($env:STEAMCMD) { $env:STEAMCMD } else { "C:\SteamCMD\steamcmd.exe" }
if (-not (Test-Path $steamCmd)) {
    Write-Error "SteamCMD not found at $steamCmd. Set STEAMCMD to steamcmd.exe path."
}

$user = $env:STEAM_USER
$pass = $env:STEAM_PASS
if (-not $user -or -not $pass) {
    Write-Error "Set STEAM_USER and STEAM_PASS (this script does not prompt for security reasons)."
}

$toolsDir = $PSScriptRoot
$repoRoot = Split-Path (Split-Path $toolsDir -Parent) -Parent
$preview = Join-Path $toolsDir "workshop_preview.png"

if (-not (Test-Path $preview)) {
    $pngB64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
    [System.IO.File]::WriteAllBytes($preview, [Convert]::FromBase64String($pngB64))
    Write-Host "Created placeholder preview: $preview"
}

function Escape-VdfPath([string]$p) {
    return $p.Replace('\', '\\')
}

$prevEsc = Escape-VdfPath $preview
$upgradeContent = Escape-VdfPath (Join-Path $repoRoot "SE_Upgrade_module_mod")
$primeContent = Escape-VdfPath (Join-Path $repoRoot "SE_Prime_Block_mod")
$primeId = if ($env:STEAM_PRIME_PUBLISHED_ID) { $env:STEAM_PRIME_PUBLISHED_ID } else { "0" }

$upgradeVdf = Join-Path $toolsDir "se_upgrade_workshop.vdf"
@"
"workshopitem"
{
	"appid"		"244850"
	"publishedfileid"	"3341019311"
	"contentfolder"		"$upgradeContent"
	"previewfile"		"$prevEsc"
	"visibility"		"0"
	"title"			"SE Upgrade Module Mod"
	"description"		"SE_mod: cockpit [Upgrade] modules."
	"changenote"		"Deploy via SteamCMD"
}
"@ | Set-Content -Path $upgradeVdf -Encoding ASCII

$primeVdf = Join-Path $toolsDir "se_prime_workshop.vdf"
@"
"workshopitem"
{
	"appid"		"244850"
	"publishedfileid"	"$primeId"
	"contentfolder"		"$primeContent"
	"previewfile"		"$prevEsc"
	"visibility"		"0"
	"title"			"SE Prime Block Mod"
	"description"		"SE_mod: Prime blocks."
	"changenote"		"Deploy via SteamCMD"
}
"@ | Set-Content -Path $primeVdf -Encoding ASCII

Write-Host "=== Workshop: SE Upgrade Module Mod (3341019311) ==="
& $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +workshop_build_item $upgradeVdf +quit
if ($LASTEXITCODE -ne 0) { Write-Error "SteamCMD failed on upgrade mod (exit $LASTEXITCODE)." }

Write-Host "=== Workshop: SE Prime Block Mod (publishedfileid=$primeId) ==="
if ($primeId -eq "0") {
    Write-Warning "publishedfileid is 0: Steam will create a NEW Workshop item. Save the printed FileID and set STEAM_PRIME_PUBLISHED_ID for future updates."
}
& $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +workshop_build_item $primeVdf +quit
if ($LASTEXITCODE -ne 0) { Write-Error "SteamCMD failed on Prime mod (exit $LASTEXITCODE)." }

Write-Host "Done."
