@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0UNINSTALL.ps1"
if errorlevel 1 (
  echo UNINSTALL FAILED. Read the error above.
  pause
  exit /b 1
)
pause
