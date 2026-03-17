@echo off
setlocal
set "SCRIPT=%~dp0deploy_deploy_run.ps1"
if not exist "%SCRIPT%" (
  echo Error: Script not found: %SCRIPT%
  exit /b 1
)
set "POWERSHELL_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%ProgramFiles%\PowerShell\7\pwsh.exe" set "POWERSHELL_EXE=%ProgramFiles%\PowerShell\7\pwsh.exe"
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
