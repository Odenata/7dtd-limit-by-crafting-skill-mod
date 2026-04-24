# Wrapper that invokes tools\deploy.ps1 from repo root (build + prepare_mod_files + copy to game).
param(
  [string]$Configuration = "Release",
  [string]$GameInstallDir = "",
  [switch]$SkipBuild,
  [string]$DllPath = ""
)

$ErrorActionPreference = "Stop"

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
  $args = @{ Configuration = $Configuration }
  if (-not [string]::IsNullOrWhiteSpace($GameInstallDir)) { $args["GameInstallDir"] = $GameInstallDir }
  if ($SkipBuild) { $args["SkipBuild"] = $true }
  if (-not [string]::IsNullOrWhiteSpace($DllPath)) { $args["DllPath"] = $DllPath }
  & $deployScript @args
  exit $LASTEXITCODE
} finally {
  Pop-Location
}
