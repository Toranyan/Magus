@echo off
setlocal enabledelayedexpansion
rem Entry point for local/CI builds on Windows.
rem Usage: build.bat ^<platform^> ^<configuration^>
rem   platform:      android, ios, windows
rem   configuration: development, release

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..\..") do set "PROJECT_PATH=%%~fI"

set "PLATFORM=%~1"
set "CONFIGURATION=%~2"

if "%PLATFORM%"=="" goto usage
if "%CONFIGURATION%"=="" goto usage

set "PLATFORM_METHOD="
if /I "%PLATFORM%"=="android" set "PLATFORM_METHOD=Android"
if /I "%PLATFORM%"=="ios" set "PLATFORM_METHOD=IOS"
if /I "%PLATFORM%"=="windows" set "PLATFORM_METHOD=Windows"

if "%PLATFORM_METHOD%"=="" (
    echo Error: unknown platform "%PLATFORM%". Expected android, ios, or windows.
    exit /b 1
)

set "CONFIGURATION_METHOD="
if /I "%CONFIGURATION%"=="development" set "CONFIGURATION_METHOD=Development"
if /I "%CONFIGURATION%"=="release" set "CONFIGURATION_METHOD=Release"

if "%CONFIGURATION_METHOD%"=="" (
    echo Error: unknown configuration "%CONFIGURATION%". Expected development or release.
    exit /b 1
)

set "BUILD_METHOD=magus.build.MagusBuild.Build%PLATFORM_METHOD%%CONFIGURATION_METHOD%"

set "UNITY_EXECUTABLE=%UNITY_PATH%"
if "%UNITY_EXECUTABLE%"=="" set "UNITY_EXECUTABLE=unity"

set "LOG_DIR=%PROJECT_PATH%\Build\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
set "LOG_FILE=%LOG_DIR%\%PLATFORM%-%CONFIGURATION%.log"

"%UNITY_EXECUTABLE%" -batchmode -quit -projectPath "%PROJECT_PATH%" -executeMethod "%BUILD_METHOD%" -logFile "%LOG_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"

echo Unity exited with code %EXIT_CODE%. Log: %LOG_FILE%
exit /b %EXIT_CODE%

:usage
echo Usage: %~nx0 ^<platform^> ^<configuration^>
echo   platform:      android, ios, windows
echo   configuration: development, release
exit /b 1
