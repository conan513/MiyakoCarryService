@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo =======================================================
echo     MiyakoCarryService - Release Build Script
echo =======================================================
echo.

echo [1/2] Tisztitas (Clean - Release)...
dotnet clean MiyakoCarryService.slnx -c Release
if errorlevel 1 goto error_exit

echo.
echo [2/2] Forditas (Build - Release: Client, Fika, Server)...
dotnet build MiyakoCarryService.slnx -c Release
if errorlevel 1 goto error_exit

echo.
echo =======================================================
echo [SUCCESS] A Release build sikeresen befejezodott!
echo =======================================================
echo.
pause
exit /b 0

:error_exit
echo.
echo =======================================================
echo [ERROR] Hiba tortent a Release forditas soran!
echo =======================================================
echo.
pause
exit /b 1
