<#
.SYNOPSIS
  Creates a CurseForge/GitHub release zip under dist\.

.DESCRIPTION
  Runs prepare_mod.ps1, stages the deployable files under a single top-level
  LimitByCraftingSkillMod\ folder, and writes LimitByCraftingSkillMod-<VERSION>.zip.
  Generated zips are release artifacts and should not be committed.

.PARAMETER Configuration
  MSBuild configuration when using the default dotnet output path (default Release).

.PARAMETER DllPath
  Override path to LimitByCraftingSkillMod.dll, usually bazel-bin\src\LimitByCraftingSkillMod.dll.

.PARAMETER ModRepoRoot
  Repository root (parent of tools). Defaults to parent of this script's directory.

.PARAMETER OutputDir
  Directory for release artifacts. Defaults to <repo>\dist.
#>
param(
  [string]$Configuration = "Release",
  [string]$DllPath = "",
  [string]$ModRepoRoot = "",
  [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ModRepoRoot)) {
  $ModRepoRoot = Split-Path $PSScriptRoot -Parent
}
$ModRepoRoot = (Resolve-Path $ModRepoRoot).Path

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
  $OutputDir = Join-Path $ModRepoRoot "dist"
}

$versionFile = Join-Path $ModRepoRoot "VERSION"
if (-not (Test-Path $versionFile)) {
  Write-Host "ERROR: VERSION not found at $versionFile" -ForegroundColor Red
  exit 1
}

$version = (Get-Content -LiteralPath $versionFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
  Write-Host "ERROR: VERSION is empty." -ForegroundColor Red
  exit 1
}

$prepareScript = Join-Path $PSScriptRoot "prepare_mod.ps1"
if (-not (Test-Path $prepareScript)) {
  Write-Host "ERROR: prepare_mod.ps1 not found at $prepareScript" -ForegroundColor Red
  exit 1
}

$prepareArgs = @{ ModRepoRoot = $ModRepoRoot; Configuration = $Configuration }
if (-not [string]::IsNullOrWhiteSpace($DllPath)) {
  $prepareArgs["DllPath"] = $DllPath
}

& $prepareScript @prepareArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$preparedDir = Join-Path $ModRepoRoot "prepared_mod_files"
if (-not (Test-Path $preparedDir)) {
  Write-Host "ERROR: prepared_mod_files not found after prepare." -ForegroundColor Red
  exit 1
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$stageRoot = Join-Path $OutputDir "_package"
$stageModDir = Join-Path $stageRoot "LimitByCraftingSkillMod"
if (Test-Path $stageRoot) {
  Remove-Item -Path $stageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stageModDir -Force | Out-Null

$requiredFiles = @(
  "LimitByCraftingSkillMod.dll",
  "0Harmony.dll",
  "ModInfo.xml",
  "Config.xml",
  "ClassNameToCraftingSkillMap.xml"
)

foreach ($file in $requiredFiles) {
  $src = Join-Path $preparedDir $file
  if (-not (Test-Path $src)) {
    Write-Host "ERROR: Missing prepared file: $src" -ForegroundColor Red
    exit 1
  }
  Copy-Item -Path $src -Destination (Join-Path $stageModDir $file) -Force
}

$zipPath = Join-Path $OutputDir "LimitByCraftingSkillMod-$version.zip"
if (Test-Path $zipPath) {
  Remove-Item -Path $zipPath -Force
}

Compress-Archive -Path $stageModDir -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item -Path $stageRoot -Recurse -Force

Write-Host "`nRelease package created:" -ForegroundColor Cyan
Write-Host $zipPath -ForegroundColor Yellow
Write-Host "Zip root: LimitByCraftingSkillMod/" -ForegroundColor Cyan
exit 0
