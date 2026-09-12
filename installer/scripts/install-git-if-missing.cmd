@echo off
setlocal

where git >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    exit /b 0
)

where winget >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    exit /b 0
)

winget install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements --silent
exit /b 0
