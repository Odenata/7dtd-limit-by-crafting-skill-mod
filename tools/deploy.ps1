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

$GameInstallDir = $GameInstallDir.TrimEnd('\', '/')
$modRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
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
$tfpHarmony = Join-Path $GameInstallDir "Mods\0_TFP_Harmony"
if (-not (Test-Path $tfpHarmony)) {
  Write-Host "WARNING: Mods\0_TFP_Harmony not found under $GameInstallDir" -ForegroundColor Yellow
  Write-Host "  LimitByCraftingSkillMod requires the official Harmony wrapper. Verify Steam game files if missing." -ForegroundColor Yellow
}

if (-not (Test-Path $modPath)) {
  New-Item -ItemType Directory -Path $modPath -Force | Out-Null
  Write-Host "Created $modPath" -ForegroundColor Green
}

$copied = $false
Get-ChildItem -Path $preparedDir -File | Where-Object { $_.Name -ne "README.md" } | ForEach-Object {
  # Preserve existing Config.xml / ModSettings.xml unless missing (user settings).
  if ($_.Name -eq "Config.xml" -or $_.Name -eq "ModSettings.xml") {
    $dst = Join-Path $modPath $_.Name
    if (Test-Path $dst) {
      Write-Host "Kept existing $($_.Name)" -ForegroundColor Cyan
      $copied = $true
      return
    }
  }
  Copy-Item -Path $_.FullName -Destination (Join-Path $modPath $_.Name) -Force -ErrorAction Stop
  Write-Host "Copied $($_.Name) -> $modPath" -ForegroundColor Green
  $copied = $true
}

# Copy Config\Localization.csv (and any other prepared subdirs except README).
Get-ChildItem -Path $preparedDir -Directory | ForEach-Object {
  Get-ChildItem -Path $_.FullName -File -Recurse | ForEach-Object {
    $rel = $_.FullName.Substring($preparedDir.Length).TrimStart('\', '/')
    $destFile = Join-Path $modPath $rel
    $destDir = Split-Path $destFile -Parent
    if (-not (Test-Path $destDir)) {
      New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item -Path $_.FullName -Destination $destFile -Force -ErrorAction Stop
    Write-Host "Copied $rel -> $modPath" -ForegroundColor Green
    $copied = $true
  }
}

# Remove mistaken root-level Localization copies from older deploy path bugs.
$staleLoc = Join-Path $modPath "ocalization.csv"
if (Test-Path $staleLoc) {
  Remove-Item -LiteralPath $staleLoc -Force
  Write-Host "Removed leftover ocalization.csv" -ForegroundColor Yellow
}

if (-not $copied) {
  Write-Host "ERROR: No deployable files in prepared_mod_files (run prepare_mod.ps1?)." -ForegroundColor Red
  exit 1
}

$staleHarmony = Join-Path $modPath "0Harmony.dll"
if (Test-Path $staleHarmony) {
  Remove-Item -LiteralPath $staleHarmony -Force
  Write-Host "Removed leftover 0Harmony.dll (use Mods\0_TFP_Harmony instead)" -ForegroundColor Yellow
}

Write-Host "`nDeployment complete." -ForegroundColor Cyan
Write-Host "Mod folder: $modPath" -ForegroundColor Yellow
Write-Host "Runtime Harmony: Mods\0_TFP_Harmony (not bundled)" -ForegroundColor Yellow
Write-Host "Restart 7 Days to Die to load the mod." -ForegroundColor Cyan
exit 0
