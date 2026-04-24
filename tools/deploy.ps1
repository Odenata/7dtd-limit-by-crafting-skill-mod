param(
  [string]$Configuration = "Release",
  # Optional explicit game directory; when empty, uses SEVENDTD_GAME_PATH, then 7_DAYS_TO_DIE_GAME_PATH, then Steam default.
  [string]$GameInstallDir = "",
  [switch]$SkipBuild,
  # Optional path to LimitByCraftingSkillMod.dll (e.g. after bazel build //src:LimitByCraftingSkillMod).
  [string]$DllPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GameInstallDir)) {
  $GameInstallDir = if (-not [string]::IsNullOrWhiteSpace($env:SEVENDTD_GAME_PATH)) { $env:SEVENDTD_GAME_PATH }
    elseif (-not [string]::IsNullOrWhiteSpace($env:7_DAYS_TO_DIE_GAME_PATH)) { $env:7_DAYS_TO_DIE_GAME_PATH }
    else { "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die" }
}

$modRepoRoot = Join-Path $PSScriptRoot ".."
$prepareScript = Join-Path $PSScriptRoot "prepare_mod.ps1"

if (-not $SkipBuild) {
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

if (-not (Test-Path $prepareScript)) {
  Write-Host "ERROR: prepare_mod.ps1 not found at $prepareScript" -ForegroundColor Red
  exit 1
}

$prepArgs = @{ ModRepoRoot = $modRepoRoot; Configuration = $Configuration }
if (-not [string]::IsNullOrWhiteSpace($DllPath)) {
  $prepArgs["DllPath"] = $DllPath
}
& $prepareScript @prepArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$preparedDir = Join-Path $modRepoRoot "prepared_mod_files"
if (-not (Test-Path $preparedDir)) {
  Write-Host "ERROR: prepared_mod_files not found after prepare." -ForegroundColor Red
  exit 1
}

$modPath = Join-Path $GameInstallDir "Mods\LimitByCraftingSkillMod"
if (-not (Test-Path $modPath)) {
  New-Item -ItemType Directory -Path $modPath -Force | Out-Null
  Write-Host "Created $modPath" -ForegroundColor Green
}

$copied = $false
Get-ChildItem -Path $preparedDir -File | Where-Object { $_.Name -ne "README.md" } | ForEach-Object {
  Copy-Item -Path $_.FullName -Destination (Join-Path $modPath $_.Name) -Force -ErrorAction Stop
  Write-Host "Copied $($_.Name) -> $modPath" -ForegroundColor Green
  $copied = $true
}

if (-not $copied) {
  Write-Host "ERROR: No deployable files in prepared_mod_files (run prepare_mod.ps1?)." -ForegroundColor Red
  exit 1
}

Write-Host "`nDeployment complete." -ForegroundColor Cyan
Write-Host "Mod folder: $modPath" -ForegroundColor Yellow
Write-Host "Restart 7 Days to Die to load the mod." -ForegroundColor Cyan
exit 0
