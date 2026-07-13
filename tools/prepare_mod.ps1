<#
.SYNOPSIS
  Assembles everything that belongs in the game's Mods\LimitByCraftingSkillMod folder into prepared_mod_files\.

.DESCRIPTION
  Copies LimitByCraftingSkillMod.dll, GearsAPI.dll, ModInfo.xml, Config.xml, ModSettings.xml,
  ClassNameToCraftingSkillMap.xml, icon.png, and Config\Localization.csv.
  Runtime Harmony comes from the game's official Mods\0_TFP_Harmony folder — do not ship 0Harmony.dll.
  End users can copy all files from prepared_mod_files into their game Mods folder after running
  this script (developers usually run deploy.ps1 which builds + prepares).

.PARAMETER Configuration
  MSBuild configuration when using the default dotnet output path (default Release).

.PARAMETER DllPath
  Override path to LimitByCraftingSkillMod.dll (e.g. bazel-bin\src\LimitByCraftingSkillMod.dll).

.PARAMETER ModRepoRoot
  Repository root (parent of tools). Defaults to parent of this script's directory.
#>
param(
  [string]$Configuration = "Release",
  [string]$DllPath = "",
  [string]$ModRepoRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ModRepoRoot)) {
  $ModRepoRoot = Split-Path $PSScriptRoot -Parent
}

if ([string]::IsNullOrWhiteSpace($DllPath)) {
  $DllPath = Join-Path $ModRepoRoot "src\bin\$Configuration\net48\LimitByCraftingSkillMod.dll"
}

if (-not (Test-Path $DllPath)) {
  Write-Host "ERROR: Mod DLL not found: $DllPath" -ForegroundColor Red
  Write-Host "Build first: dotnet build `"$ModRepoRoot\src\LimitByCraftingSkillMod.csproj`" -c $Configuration" -ForegroundColor Yellow
  exit 1
}

$outDir = Join-Path $ModRepoRoot "prepared_mod_files"
if (-not (Test-Path $outDir)) {
  New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

Get-ChildItem -Path $outDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -ne "README.md" } | Remove-Item -Force
Get-ChildItem -Path $outDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force

$srcDir = Join-Path $ModRepoRoot "src"
$gearsApi = Join-Path $ModRepoRoot "third_party\gearsapi\lib\net472\GearsAPI.dll"
$files = @(
  @{ Src = $DllPath; Dest = "LimitByCraftingSkillMod.dll" }
  @{ Src = $gearsApi; Dest = "GearsAPI.dll" }
  @{ Src = Join-Path $srcDir "ModInfo.xml"; Dest = "ModInfo.xml" }
  @{ Src = Join-Path $srcDir "Config.xml"; Dest = "Config.xml" }
  @{ Src = Join-Path $srcDir "ModSettings.xml"; Dest = "ModSettings.xml" }
  @{ Src = Join-Path $srcDir "ClassNameToCraftingSkillMap.xml"; Dest = "ClassNameToCraftingSkillMap.xml" }
  @{ Src = Join-Path $ModRepoRoot "icon.png"; Dest = "icon.png" }
)

$locSrc = Join-Path $srcDir "Config\Localization.csv"
$locDestDir = Join-Path $outDir "Config"
if (-not (Test-Path $locDestDir)) {
  New-Item -ItemType Directory -Path $locDestDir -Force | Out-Null
}
if (-not (Test-Path $locSrc)) {
  Write-Host "ERROR: Missing $locSrc" -ForegroundColor Red
  exit 1
}
Copy-Item -Path $locSrc -Destination (Join-Path $locDestDir "Localization.csv") -Force
Write-Host "Prepared Config\Localization.csv" -ForegroundColor Green

foreach ($f in $files) {
  if (-not (Test-Path $f.Src)) {
    Write-Host "ERROR: Missing $($f.Src)" -ForegroundColor Red
    exit 1
  }
  Copy-Item -Path $f.Src -Destination (Join-Path $outDir $f.Dest) -Force
  Write-Host "Prepared $($f.Dest)" -ForegroundColor Green
}

Write-Host "`nOutput: $outDir" -ForegroundColor Cyan
Write-Host "Copy these files into: <game>\\Mods\\LimitByCraftingSkillMod\\" -ForegroundColor Cyan
Write-Host "Requires game Mods\\0_TFP_Harmony (do not add 0Harmony.dll here)." -ForegroundColor Cyan
exit 0
