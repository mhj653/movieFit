@echo off
setlocal

set ROOT=%~dp0
set SLN=%ROOT%ProcessVideoAnalyzer.sln
set DOTNET=dotnet

where dotnet >nul 2>nul
if errorlevel 1 (
  if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set DOTNET=%ProgramFiles%\dotnet\dotnet.exe
  ) else (
    echo dotnet was not found.
    echo Install Visual Studio 2022 with the .NET desktop development workload and .NET 8 SDK.
    exit /b 1
  )
)

"%DOTNET%" --list-sdks | findstr /R "^8\." >nul
if errorlevel 1 (
  echo .NET 8 SDK was not found.
  echo Install .NET 8 SDK or enable it through Visual Studio Installer.
  exit /b 1
)

"%DOTNET%" restore "%SLN%"
if errorlevel 1 exit /b 1

"%DOTNET%" build "%SLN%" -c Debug -p:Platform=x64 --no-restore
if errorlevel 1 exit /b 1

echo.
echo Build succeeded.
