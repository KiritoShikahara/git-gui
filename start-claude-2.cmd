@echo off
setlocal

cd /d "%~dp0"

set "CLAUDE_EXE=%USERPROFILE%\.local\bin\claude.exe"
set "CLAUDE_CONFIG_DIR=%USERPROFILE%\.claude-account2"

if not exist "%CLAUDE_EXE%" (
  echo Claude Code was not found:
  echo %CLAUDE_EXE%
  pause
  exit /b 1
)

if not exist "%CLAUDE_CONFIG_DIR%" (
  mkdir "%CLAUDE_CONFIG_DIR%"
)

rem --- Agent Memory: deploy the context-reduction layer (idempotent, non-fatal) ---
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0agent-memory\installer\install.ps1" -ConfigDir "%CLAUDE_CONFIG_DIR%"

echo.
echo ========================================
echo Claude Code Account 2
echo Config: %CLAUDE_CONFIG_DIR%
echo ========================================
echo.

"%CLAUDE_EXE%" --dangerously-skip-permissions %*

set "EXIT_CODE=%ERRORLEVEL%"

echo.
echo Claude Code exited with code %EXIT_CODE%.

pause
exit /b %EXIT_CODE%