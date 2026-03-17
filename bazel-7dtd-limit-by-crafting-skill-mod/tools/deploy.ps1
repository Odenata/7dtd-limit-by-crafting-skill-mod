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
$exitAfterDevTools = $LASTEXITCODE
# Only exit on explicit non-zero; $LASTEXITCODE can be $null after & script (treated as success)
if ($null -ne $exitAfterDevTools -and $exitAfterDevTools -ne 0) { exit $exitAfterDevTools }

$modPath = Join-Path $GameInstallDir "Mods\LimitByCraftingSkillMod"
$mapSrc = Join-Path $modRepoRoot "src\ClassNameToCraftingSkillMap.xml"
if (Test-Path $mapSrc) {
  Copy-Item $mapSrc -Destination $modPath -Force -ErrorAction Stop
  Write-Host "Copied ClassNameToCraftingSkillMap.xml to $modPath" -ForegroundColor Green
} else {
  Write-Host "WARNING: ClassNameToCraftingSkillMap.xml not found at $mapSrc" -ForegroundColor Yellow
}
exit 0
