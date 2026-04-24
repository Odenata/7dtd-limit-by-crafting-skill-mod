<#
.SYNOPSIS
  Assembles everything that belongs in the game's Mods\LimitByCraftingSkillMod folder into prepared_mod_files\.

.DESCRIPTION
  Copies LimitByCraftingSkillMod.dll, 0Harmony.dll (from 7dtd-mod-dev-tools), ModInfo.xml, Config.xml,
  and ClassNameToCraftingSkillMap.xml. End users can copy all files from prepared_mod_files into their
  game Mods folder after running this script (developers usually run deploy.ps1 which builds + prepares).

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

$devToolsHarmony = Join-Path $ModRepoRoot "..\7dtd-mod-dev-tools\third_party\harmony\lib\net472\0Harmony.dll"
if (-not (Test-Path $devToolsHarmony)) {
  Write-Host "ERROR: 0Harmony.dll not found at $devToolsHarmony (is 7dtd-mod-dev-tools a sibling repo?)" -ForegroundColor Red
  exit 1
}

if ([string]::IsNullOrWhiteSpace($DllPath)) {
  $DllPath = Join-Path $ModRepoRoot "src\bin\$Configuration\net472\LimitByCraftingSkillMod.dll"
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

$srcDir = Join-Path $ModRepoRoot "src"
$files = @(
  @{ Src = $DllPath; Dest = "LimitByCraftingSkillMod.dll" }
  @{ Src = $devToolsHarmony; Dest = "0Harmony.dll" }
  @{ Src = Join-Path $srcDir "ModInfo.xml"; Dest = "ModInfo.xml" }
  @{ Src = Join-Path $srcDir "Config.xml"; Dest = "Config.xml" }
  @{ Src = Join-Path $srcDir "ClassNameToCraftingSkillMap.xml"; Dest = "ClassNameToCraftingSkillMap.xml" }
)

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
exit 0
