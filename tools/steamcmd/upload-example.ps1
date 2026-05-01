# Example: publish SE_Upgrade_module_mod to Steam Workshop via SteamCMD.
# Copy se_upgrade_workshop.vdf.example to se_upgrade_workshop.vdf, fill absolute paths, then:
#   .\upload-example.ps1 -Mod upgrade
# Or set $env:STEAM_USER / prompt for password on first run.

param(
    [ValidateSet("upgrade", "prime")]
    [string]$Mod = "upgrade",
    [string]$SteamCmd = "C:\SteamCMD\steamcmd.exe"
)

$ErrorActionPreference = "Stop"
$vdfName = if ($Mod -eq "upgrade") { "se_upgrade_workshop.vdf" } else { "se_prime_workshop.vdf" }
$vdfPath = Join-Path $PSScriptRoot $vdfName

if (-not (Test-Path $vdfPath)) {
    Write-Error "Missing $vdfPath — copy the matching .vdf.example to $vdfName and edit paths/ids."
}
if (-not (Test-Path $SteamCmd)) {
    Write-Error "SteamCMD not found at $SteamCmd. Set -SteamCmd to your steamcmd.exe path."
}

$user = $env:STEAM_USER
if (-not $user) {
    $user = Read-Host "Steam username"
}

$pass = $env:STEAM_PASS
if ($pass) {
    Write-Host "Running: $SteamCmd +login <user> +workshop_build_item ... (password from STEAM_PASS)"
    & $SteamCmd +login $user $pass +workshop_build_item $vdfPath +quit
} else {
    Write-Host "Running: $SteamCmd +login (interactive password) +workshop_build_item $vdfPath +quit"
    & $SteamCmd +login $user +workshop_build_item $vdfPath +quit
}
