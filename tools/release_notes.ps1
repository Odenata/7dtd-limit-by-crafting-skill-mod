# Release notes wrapper — calls sibling 7dtd-mod-dev-tools script

param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$script = Join-Path $repoRoot "..\7dtd-mod-dev-tools\tools\release-notes\release_notes.py"
if (-not (Test-Path -LiteralPath $script)) {
  Write-Host "ERROR: release_notes.py not found at $script" -ForegroundColor Red
  Write-Host "  Expect sibling checkout: repos\7dtd-mod-dev-tools" -ForegroundColor Yellow
  exit 1
}

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
  $python = Get-Command py -ErrorAction SilentlyContinue
}
if (-not $python) {
  Write-Host "ERROR: python not found on PATH" -ForegroundColor Red
  exit 1
}

& $python.Source $script --repo-root $repoRoot @RemainingArgs
exit $LASTEXITCODE
