# Wrapper that invokes tools\deploy.ps1 from repo root.
param(
  [string]$Configuration = "Release",
  [string]$GameInstallDir = $(if ($env:7_DAYS_TO_DIE_GAME_PATH) { $env:7_DAYS_TO_DIE_GAME_PATH } else { "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die" })
)

$ErrorActionPreference = "Stop"

# Prefer Bazel/IDE workspace; else current dir if it looks like this repo; else script location.
$repoRoot = if ($env:BUILD_WORKSPACE_DIRECTORY) { $env:BUILD_WORKSPACE_DIRECTORY }
  elseif ((Get-Location).Path -and (Test-Path (Join-Path (Get-Location).Path "src\LimitByCraftingSkillMod.csproj"))) { (Get-Location).Path }
  else { Split-Path $PSScriptRoot -Parent }
$deployScript = Join-Path $repoRoot "tools\deploy.ps1"
if (-not (Test-Path $deployScript)) {
  Write-Host "ERROR: Script not found: $deployScript" -ForegroundColor Red
  exit 1
}

Push-Location $repoRoot
try {
  & $deployScript -Configuration $Configuration -GameInstallDir $GameInstallDir
  exit $LASTEXITCODE
} finally {
  Pop-Location
}
