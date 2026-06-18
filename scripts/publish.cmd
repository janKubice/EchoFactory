@echo off
REM Build a self-contained, single-file EchoFactory build (Windows, run from cmd).
REM Usage: scripts\publish.cmd [win-x64]   (default: win-x64)
REM Output: artifacts\<rid>\ — zip and ship the whole folder. No .NET needed on the target.
setlocal
set RID=%1
if "%RID%"=="" set RID=win-x64
echo Publishing EchoFactory for %RID% ...
dotnet publish "%~dp0..\src\EchoFactory.Game\EchoFactory.Game.csproj" -c Release -r %RID% --self-contained -p:PublishSingleFile=true -o "%~dp0..\artifacts\%RID%"
if errorlevel 1 (
  echo.
  echo Publish FAILED. Make sure the .NET 8 SDK is installed: dotnet --version
  exit /b 1
)
echo.
echo Done. Ship the whole folder: artifacts\%RID%
endlocal
