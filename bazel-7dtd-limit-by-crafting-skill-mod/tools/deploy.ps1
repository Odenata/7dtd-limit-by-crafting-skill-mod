param(
  [string]$Configuration = "Release",
  [string]$GameInstallDir = $(if ($env:7_DAYS_TO_DIE_GAME_PATH) { $env:7_DAYS_TO_DIE_GAME_PATH } else { "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die" })
)

$ErrorActionPreference = "Stop"

$devTools = Join-Path $PSScriptRoot "..\..\7dtd-mod-dev-tools\tools\build-deploy\deploy.ps1"
$params = @{
  ModRepoPath = (Join-Path $PSScriptRoot "..")
  ModName = "LimitByCraftingSkillMod"
  Configuration = $Configuration
}
if (-not [string]::IsNullOrWhiteSpace($GameInstallDir)) {
  $params["GameInstallDir"] = $GameInstallDir
}
& $devTools @params
exit $LASTEXITCODE
