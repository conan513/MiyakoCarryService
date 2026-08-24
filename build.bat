@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo =======================================================
echo     MiyakoCarryService - Release Build Script
echo =======================================================
echo.

echo [1/3] Tisztitas (Clean - Release)...
dotnet clean MiyakoCarryService.slnx -c Release
if errorlevel 1 goto error_exit

echo.
echo [2/3] Forditas (Build - Release: Client, Fika, Assistant, Server)...
dotnet build MiyakoCarryService.slnx -c Release
if errorlevel 1 goto error_exit

echo.
echo [3/3] Csomagolas (Zips letrehozasa: Plugin, Fika, Assistant)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ver = Get-Content 'version.txt' | Select-Object -First 1; Write-Host \"Verzio: $ver\"; Compress-Archive -Path 'Build\Plugin\*' -DestinationPath ('MiyakoCarryService-' + $ver + '.zip') -Force; Compress-Archive -Path 'Build\Fika\*' -DestinationPath ('MiyakoCarryServiceFika-' + $ver + '.zip') -Force; Compress-Archive -Path 'Build\Assistant\*' -DestinationPath ('MiyakoCarryServiceAssistant-' + $ver + '.zip') -Force"
if errorlevel 1 goto error_exit

echo.
echo =======================================================
echo [SUCCESS] A Release build es csomagolas sikeresen befejezodott!
echo =======================================================
echo.
pause
exit /b 0

:error_exit
echo.
echo =======================================================
echo [ERROR] Hiba tortent a folyamat soran!
echo =======================================================
echo.
pause
exit /b 1
