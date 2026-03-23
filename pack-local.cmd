@echo off
setlocal enabledelayedexpansion

:: ═══════════════════════════════════════════════════════════════
::  pack-local.cmd — Pack HopDev.Maui.Controls to local NuGet
::
::  Usage:  pack-local.cmd 1.0.0-local.1
::          pack-local.cmd 1.0.0          (release candidate)
:: ═══════════════════════════════════════════════════════════════

if "%~1"=="" (
    echo Usage: pack-local.cmd ^<version^>
    echo   Example: pack-local.cmd 1.0.0-local.1
    exit /b 1
)

set VERSION=%~1
set LOCAL_FEED=D:\DATA\LocalNuGet
set ARTIFACTS=artifacts\packages

echo.
echo ═══════════════════════════════════════════════════════════
echo  Packing HopDev.Maui.Controls v%VERSION%
echo  Target: %LOCAL_FEED%
echo ═══════════════════════════════════════════════════════════
echo.

:: Create output dirs
if not exist "%LOCAL_FEED%" mkdir "%LOCAL_FEED%"
if not exist "%ARTIFACTS%" mkdir "%ARTIFACTS%"

:: Clean
echo [1/4] Cleaning...
dotnet clean src\HopDev.Maui.Controls\HopDev.Maui.Controls.csproj -c Release --verbosity quiet 2>nul

:: Build
echo [2/4] Building Release...
dotnet build src\HopDev.Maui.Controls\HopDev.Maui.Controls.csproj -c Release -p:Version=%VERSION% --verbosity quiet
if errorlevel 1 goto :error

:: Pack
echo [3/4] Packing...
dotnet pack src\HopDev.Maui.Controls\HopDev.Maui.Controls.csproj -c Release -p:Version=%VERSION% --output %ARTIFACTS% --verbosity quiet
if errorlevel 1 goto :error

:: Copy to local feed
echo [4/4] Copying to %LOCAL_FEED%...
copy /y "%ARTIFACTS%\HopDev.Maui.Controls.%VERSION%.nupkg" "%LOCAL_FEED%\" >nul
copy /y "%ARTIFACTS%\HopDev.Maui.Controls.%VERSION%.snupkg" "%LOCAL_FEED%\" 2>nul

:: Clear NuGet cache for this package
echo Clearing NuGet cache for HopDev.Maui.Controls...
for /d %%D in ("%USERPROFILE%\.nuget\packages\hopdev.maui.controls*") do rmdir /s /q "%%D" 2>nul

echo.
echo ═══════════════════════════════════════════════════════════
echo  SUCCESS: HopDev.Maui.Controls v%VERSION%
echo  Location: %LOCAL_FEED%
echo ═══════════════════════════════════════════════════════════
echo.
echo Next steps:
echo   1. In consumer app, ensure nuget.config has LocalNuGet source
echo   2. Update PackageReference version to %VERSION%
echo   3. dotnet restore --force
echo.
exit /b 0

:error
echo.
echo FAILED — see errors above.
exit /b 1
