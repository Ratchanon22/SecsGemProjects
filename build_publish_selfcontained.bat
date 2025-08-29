@echo off
setlocal

REM Define output folder
set PUBLISH_DIR=publish

REM Clean previous publish folder
if exist %PUBLISH_DIR% (
    echo Cleaning previous publish folder...
    rmdir /s /q %PUBLISH_DIR%
)

REM Create publish folder
mkdir %PUBLISH_DIR%

REM Publish HostApp as self-contained
echo Publishing HostApp as self-contained...
dotnet publish HostApp\HostApp.csproj -c Release -r win-x64 --self-contained true -o %PUBLISH_DIR%

REM Publish SimulatorApp as self-contained
echo Publishing SimulatorApp as self-contained...
dotnet publish SimulatorApp\SimulatorApp.csproj -c Release -r win-x64 --self-contained true -o %PUBLISH_DIR%

REM Copy appsettings.json from root to publish folder
echo Copying appsettings.json...
copy appsettings.json %PUBLISH_DIR%\appsettings.json >nul

echo Done. Published files are in the %PUBLISH_DIR% folder.
pause