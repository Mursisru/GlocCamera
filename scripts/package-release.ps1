#Requires -Version 5.0
$ErrorActionPreference = "Stop"

# Repo root = parent of /scripts
$Root = Split-Path $PSScriptRoot -Parent

$Csproj = Join-Path $Root "GlocCamera_Engine\GlocCamera_Engine.csproj"
if (-not (Test-Path $Csproj)) {
    Write-Error "Expected project at $Csproj (run this script from the cloned repo)."
}

$PluginCs = Join-Path $Root "GlocCamera_Engine\GlocCameraPlugin.cs"
$m = Select-String -Path $PluginCs -Pattern 'const string PluginVersion = "([^"]+)"' | Select-Object -First 1
if (-not $m) {
    Write-Error "Could not parse PluginVersion from $PluginCs"
}
$Version = $m.Matches[0].Groups[1].Value

$Dll = Join-Path $Root "GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll"
if (-not (Test-Path $Dll)) {
    Write-Error "Release DLL not found: $Dll`nBuild Release configuration first."
}

$ReleaseDir = Join-Path $Root "release"
if (-not (Test-Path $ReleaseDir)) {
    New-Item -ItemType Directory -Path $ReleaseDir | Out-Null
}

$ZipName = "GlocCamera_Engine_v$Version.zip"
$ZipPath = Join-Path $ReleaseDir $ZipName
$InstallTxt = Join-Path $ReleaseDir "INSTALL.txt"
if (-not (Test-Path $InstallTxt)) {
    Write-Error "Missing $InstallTxt"
}

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

Compress-Archive -Path $Dll, $InstallTxt -DestinationPath $ZipPath -Force
Write-Host "Wrote $ZipPath"
