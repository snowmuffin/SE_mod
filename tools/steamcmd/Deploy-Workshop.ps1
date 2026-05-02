# Deploy both SE mods to Space Engineers Steam Workshop (AppID 244850).
# Requires: SteamCMD, Space Engineers on the account, and valid credentials.
#
# Usage (PowerShell):
#   .\Deploy-Workshop.ps1
#   If STEAM_USER / STEAM_PASS are not set, you will be prompted (password is hidden).
#   Or non-interactive: $env:STEAM_USER = "..."; $env:STEAM_PASS = "..."
#
# Optional:
#   $env:STEAMCMD = "D:\SteamCMD\steamcmd.exe"   # default: C:\SteamCMD\steamcmd.exe
#   $env:STEAM_PRIME_PUBLISHED_ID = "0"          # default 0 = create NEW Prime item; set to existing id to update
#   $env:STEAM_GUARD = "12345"                    # optional; else prompted when empty (Enter to skip)

$ErrorActionPreference = "Stop"

function ConvertFrom-SecureStringPlain {
    param([System.Security.SecureString]$SecureString)
    if ($null -eq $SecureString) { return $null }
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null
    }
}

function Get-SteamLogin {
    $user = $env:STEAM_USER
    if ([string]::IsNullOrWhiteSpace($user)) {
        $user = Read-Host "Steam account name (login)"
    }
    if ([string]::IsNullOrWhiteSpace($user)) {
        Write-Error "Steam login name is required."
    }

    $pass = $env:STEAM_PASS
    if ([string]::IsNullOrWhiteSpace($pass)) {
        $sec = Read-Host "Steam password" -AsSecureString
        $pass = ConvertFrom-SecureStringPlain -SecureString $sec
        $sec.Dispose()
    }
    if ([string]::IsNullOrWhiteSpace($pass)) {
        Write-Error "Steam password is required."
    }

    return @{ User = $user; Pass = $pass }
}

$steamCmd = if ($env:STEAMCMD) { $env:STEAMCMD } else { "C:\SteamCMD\steamcmd.exe" }
if (-not (Test-Path $steamCmd)) {
    Write-Error "SteamCMD not found at $steamCmd. Set STEAMCMD to steamcmd.exe path."
}

$login = Get-SteamLogin
$user = $login.User
$pass = $login.Pass

$guard = $env:STEAM_GUARD
if ([string]::IsNullOrWhiteSpace($guard)) {
    $guard = Read-Host "Steam Guard code from email or app (Enter to skip)"
}

function Invoke-SteamWorkshopBuild {
    param(
        [Parameter(Mandatory)][string]$VdfPath
    )
    if ([string]::IsNullOrWhiteSpace($guard)) {
        & $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +workshop_build_item $VdfPath +quit
    }
    else {
        & $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +set_steam_guard_code $guard +workshop_build_item $VdfPath +quit
    }
}

$toolsDir = $PSScriptRoot
$repoRoot = Split-Path (Split-Path $toolsDir -Parent) -Parent
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
Invoke-SteamWorkshopBuild -VdfPath $upgradeVdf
if ($LASTEXITCODE -ne 0) { Write-Error "SteamCMD failed on upgrade mod (exit $LASTEXITCODE)." }

Write-Host "=== Workshop: SE Prime Block Mod (publishedfileid=$primeId) ==="
if ($primeId -eq "0") {
    Write-Warning "publishedfileid is 0: Steam will create a NEW Workshop item. Save the printed FileID and set STEAM_PRIME_PUBLISHED_ID for future updates."
}
Invoke-SteamWorkshopBuild -VdfPath $primeVdf
if ($LASTEXITCODE -ne 0) { Write-Error "SteamCMD failed on Prime mod (exit $LASTEXITCODE)." }

Write-Host "Done."
