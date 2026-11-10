@echo off
setlocal
title XMW UL Category Backpack v0.3.4 - Use-based taxonomy
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL.ps1"
set "XMW_EXIT=%ERRORLEVEL%"
if not "%XMW_EXIT%"=="0" (
  echo.
  echo INSTALL FAILED. Send INSTALL_LAST.log in this extracted folder.
  echo Do not manually copy staging files to Mods.
)
pause
exit /b %XMW_EXIT%
