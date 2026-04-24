# Wrapper for bazel run //tools:prepare_mod — runs tools\prepare_mod.ps1 from repo root.
param(
  [string]$Configuration = "Release",
  [string]$DllPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = if ($env:BUILD_WORKSPACE_DIRECTORY) { $env:BUILD_WORKSPACE_DIRECTORY }
  elseif ((Get-Location).Path -and (Test-Path (Join-Path (Get-Location).Path "src\LimitByCraftingSkillMod.csproj"))) { (Get-Location).Path }
  else { Split-Path $PSScriptRoot -Parent }
$script = Join-Path $repoRoot "tools\prepare_mod.ps1"
if (-not (Test-Path $script)) {
  Write-Host "ERROR: Script not found: $script" -ForegroundColor Red
  exit 1
}

Push-Location $repoRoot
try {
  if (-not [string]::IsNullOrWhiteSpace($DllPath)) {
    & $script -Configuration $Configuration -DllPath $DllPath -ModRepoRoot $repoRoot
  } else {
    & $script -Configuration $Configuration -ModRepoRoot $repoRoot
  }
  exit $LASTEXITCODE
} finally {
  Pop-Location
}
