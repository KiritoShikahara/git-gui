@echo off
setlocal
cd /d "%~dp0.."

echo === Publishing GitGui.App (self-contained, single file) ===
dotnet publish src\GitGui.App\GitGui.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o exe
if errorlevel 1 exit /b 1

echo === Building installer (GitGuiSetup.msi) ===
dotnet build installer\GitGui.Installer.wixproj -c Release
if errorlevel 1 exit /b 1

echo.
echo Done. Installer: installer\bin\Release\GitGuiSetup.msi
