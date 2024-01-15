@echo off
setlocal enabledelayedexpansion

net session >nul 2>&1 > output.txt
if %errorlevel% == 0 (
   echo Running as admin >> output.txt
) else (
   echo Try to run as admin... >> output.txt
        powershell start -verb runas '%~0' %* >> output.txt
    exit /b
)
@echo off
:: Save current path
set ORIGINAL_PATH=%~dp0

:: changing path to oryginal
cd /d %ORIGINAL_PATH%

echo Continuing will delete the existing service and reinstall it. Admin rights required.
pause

:: Deleting the existing service if it exists
sc delete TerminalMonitoringService
timeout 1

:: Retrieving the script's path and setting the path to the .exe file
set "scriptPath=%~dp0"
set "exePath=%scriptPath%TerminalMonitoringService.exe"
echo !exePath!
pause
:: Creating and starting the new service
sc create "TerminalMonitoringService" binPath= "%exePath%" start= auto
timeout 1
sc start TerminalMonitoringService

echo Operation completed. >> output.txt
pause
