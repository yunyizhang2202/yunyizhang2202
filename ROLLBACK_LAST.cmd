@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0ROLLBACK_LAST.ps1"
set "XMW_EXIT=%ERRORLEVEL%"
pause
exit /b %XMW_EXIT%
