@echo off
setlocal enabledelayedexpansion

REM Set Development environment so appsettings.Development.json is loaded
set "ASPNETCORE_ENVIRONMENT=Development"

REM Set paths relative to batch script directory (current folder)
set "PROJECT_DIR=%~dp0"

REM Silent pre-flight check for trusted HTTPS developer certificate
dotnet dev-certs https --check --trust >nul 2>&1
if %errorlevel% neq 0 (
    echo [Pre-flight] Trusted HTTPS dev certificate missing or un-trusted. Initializing...
    dotnet dev-certs https --trust
)


REM Check for Agent Mode argument
if "%~1"=="--agent" (
    echo [Agent Mode] Launching application with low verbosity...
    dotnet run --project "%PROJECT_DIR%." --launch-profile https --verbosity quiet
) else if "%~1"=="/agent" (
    echo [Agent Mode] Launching application with low verbosity...
    dotnet run --project "%PROJECT_DIR%." --launch-profile https --verbosity quiet
) else (
    echo [User Mode] Launching application...
    dotnet run --project "%PROJECT_DIR%." --launch-profile https
)
