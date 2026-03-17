param(
  [string]$Configuration = "Release",
  [string]$GameInstallDir = $(if ($env:7_DAYS_TO_DIE_GAME_PATH) { $env:7_DAYS_TO_DIE_GAME_PATH } else { "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die" })
)

$ErrorActionPreference = "Stop"

$modRepoRoot = Join-Path $PSScriptRoot ".."
$dllPath = "src\bin\$Configuration\net472\LimitByCraftingSkillMod.dll"

# Build first with our csproj so dev-tools deploy does not run its default build (GameApiExporter.csproj).
if (-not (Test-Path $dllPath)) {
  $buildScript = Join-Path $PSScriptRoot "build.ps1"
  if (-not (Test-Path $buildScript)) {
    Write-Host "ERROR: build.ps1 not found at $buildScript" -ForegroundColor Red
    exit 1
  }
  & $buildScript -Configuration $Configuration
  if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
  }
}

$devTools = Join-Path $PSScriptRoot "..\..\7dtd-mod-dev-tools\tools\build-deploy\deploy.ps1"
$params = @{
  ModRepoPath = $modRepoRoot
  ModName = "LimitByCraftingSkillMod"
  Configuration = $Configuration
  DllPath = (Join-Path $modRepoRoot $dllPath)
}
if (-not [string]::IsNullOrWhiteSpace($GameInstallDir)) {
  $params["GameInstallDir"] = $GameInstallDir
}
& $devTools @params
exit $LASTEXITCODE
