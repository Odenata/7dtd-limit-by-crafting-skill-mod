# Wrapper for bazel run //tools:build. Prints non-hermetic warning and invokes tools\build.ps1 from repo root.
param(
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARNING: This build is NOT hermetic. Bazel does not control:" -ForegroundColor Yellow
Write-Host "  - Host dotnet and .NET Framework 4.7.2 reference assemblies" -ForegroundColor Yellow
Write-Host "  - Sibling repo 7dtd-mod-dev-tools (relative path)" -ForegroundColor Yellow
Write-Host "  - Output may be locked if another process holds the DLL" -ForegroundColor Yellow
Write-Host "For a hermetic DLL use: bazel build //src:LimitByCraftingSkillMod" -ForegroundColor Cyan
Write-Host ""

$repoRoot = if ($env:BUILD_WORKSPACE_DIRECTORY) { $env:BUILD_WORKSPACE_DIRECTORY } else { Split-Path $PSScriptRoot -Parent }
$buildScript = Join-Path $repoRoot "tools\build.ps1"
if (-not (Test-Path $buildScript)) {
  Write-Host "ERROR: Script not found: $buildScript" -ForegroundColor Red
  exit 1
}

Push-Location $repoRoot
try {
  & $buildScript -Configuration $Configuration
  exit $LASTEXITCODE
} finally {
  Pop-Location
}
