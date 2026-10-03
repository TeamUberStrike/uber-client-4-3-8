@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-contract.ps1" %*
exit /b %ERRORLEVEL%
