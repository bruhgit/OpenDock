# OpenDock Release Build & Packaging Script
# Publishes a self-contained, single-file Windows x64 executable and packages the GitHub Release zip.

param(
    [string]$VersionOverride = ""
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$csprojPath = Join-Path $rootDir "OpenDock\OpenDock.csproj"

Write-Host "==> Reading version from OpenDock.csproj..." -ForegroundColor Cyan
[xml]$csprojXml = Get-Content $csprojPath
$detectedVersion = $csprojXml.Project.PropertyGroup.Version | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

$version = if ($VersionOverride) { $VersionOverride } elseif ($detectedVersion) { $detectedVersion } else { "6.0.2" }
Write-Host "Target Version: $version" -ForegroundColor Green

$publishDir = Join-Path $rootDir "OpenDock\bin\Publish"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "`n==> Publishing Self-Contained Single-File Executable (win-x64)..." -ForegroundColor Cyan
dotnet publish $csprojPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=false -o $publishDir

if (-not (Test-Path (Join-Path $publishDir "OpenDock.exe"))) {
    throw "Publish failed: OpenDock.exe not found in $publishDir"
}

$stagingDir = Join-Path $rootDir "OpenDock\bin\Release-Package"
if (Test-Path $stagingDir) {
    Remove-Item $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

Write-Host "`n==> Staging release files..." -ForegroundColor Cyan
Copy-Item (Join-Path $publishDir "OpenDock.exe") -Destination $stagingDir
if (Test-Path (Join-Path $publishDir "icon.png")) { Copy-Item (Join-Path $publishDir "icon.png") -Destination $stagingDir }
if (Test-Path (Join-Path $publishDir "folder.png")) { Copy-Item (Join-Path $publishDir "folder.png") -Destination $stagingDir }
if (Test-Path (Join-Path $publishDir "opendock.css")) { Copy-Item (Join-Path $publishDir "opendock.css") -Destination $stagingDir }

$zipPath = Join-Path $rootDir "OpenDock-v$version.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "`n==> Creating release archive: $zipPath..." -ForegroundColor Cyan
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipItem = Get-Item $zipPath
$exeItem = Get-Item (Join-Path $stagingDir "OpenDock.exe")

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] OpenDock Release Package Ready for GitHub!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "Executable  : $($exeItem.FullName) ($([math]::Round($exeItem.Length / 1MB, 2)) MB)"
Write-Host "Release Zip : $($zipItem.FullName) ($([math]::Round($zipItem.Length / 1MB, 2)) MB)"
Write-Host "Contents    :"
Get-ChildItem $stagingDir | ForEach-Object {
    Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1KB, 1)) KB)"
}
Write-Host "`nReady to upload to GitHub Releases. Users will NOT require .NET Desktop Runtime!" -ForegroundColor Green
