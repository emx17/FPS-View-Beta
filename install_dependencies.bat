@echo off
title emx17 FPS Viewer - Setup Wizard
color 0A

echo ===================================================
echo       emx17 FPS Viewer Beta - Required Library Setup
echo ===================================================
echo.
echo Downloading NuGet packages required for the project (TraceEvent, WMI, etc.)...
echo Please wait...
echo.

dotnet restore

if %errorlevel% neq 0 (
    echo.
    color 0C
    echo [ERROR] A problem occurred while downloading libraries.
    echo Please make sure that .NET SDK 10.0 is installed on your computer.
) else (
    echo.
    echo [SUCCESS] All requirements have been successfully installed!
    echo You can now use emx17 FPS Viewer Beta    
)

echo.
pause