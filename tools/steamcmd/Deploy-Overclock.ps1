# Upload SE_Unified_mod to Steam Workshop as "SE Overclock" (AppID 244850).
#
# Usage:
#   .\Deploy-Overclock.ps1
# Optional env: STEAM_USER, STEAM_PASS, STEAM_GUARD, STEAMCMD
#   STEAM_OVERCLOCK_PUBLISHED_ID = existing Workshop file id to update; default "0" creates a new item.

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
    $guard = Read-Host "Steam Guard code (Enter to skip)"
}

$toolsDir = $PSScriptRoot
$repoRoot = Split-Path (Split-Path $toolsDir -Parent) -Parent
$preview = Join-Path $toolsDir "workshop_preview.png"
if (-not (Test-Path $preview)) {
    $pngB64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
    [System.IO.File]::WriteAllBytes($preview, [Convert]::FromBase64String($pngB64))
}

function Escape-VdfPath([string]$p) { return $p.Replace('\', '\\') }

$prevEsc = Escape-VdfPath $preview
$contentEsc = Escape-VdfPath (Join-Path $repoRoot "SE_Unified_mod")
$pubId = if ($env:STEAM_OVERCLOCK_PUBLISHED_ID) { $env:STEAM_OVERCLOCK_PUBLISHED_ID } else { "0" }

$vdfPath = Join-Path $toolsDir "se_overclock_workshop.vdf"
@"
"workshopitem"
{
	"appid"		"244850"
	"publishedfileid"	"$pubId"
	"contentfolder"		"$contentEsc"
	"previewfile"		"$prevEsc"
	"visibility"		"0"
	"title"			"SE Overclock"
	"description"		"Upgrade modules + Prime blocks (unified). Repo: SE_mod SE_Unified_mod."
	"changenote"		"SE Overclock — SteamCMD publish"
}
"@ | Set-Content -Path $vdfPath -Encoding ASCII

Write-Host "Workshop VDF: $vdfPath"
Write-Host "Content: $(Join-Path $repoRoot 'SE_Unified_mod')"
if ($pubId -eq "0") {
    Write-Warning "publishedfileid is 0: Steam creates a NEW item. Note the FileID from output and set STEAM_OVERCLOCK_PUBLISHED_ID for next run."
}

if ([string]::IsNullOrWhiteSpace($guard)) {
    & $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +workshop_build_item $vdfPath +quit
} else {
    & $steamCmd +@ShutdownOnFailedCommand 1 +login $user $pass +set_steam_guard_code $guard +workshop_build_item $vdfPath +quit
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "SteamCMD exited with code $LASTEXITCODE"
}
Write-Host "Done."
