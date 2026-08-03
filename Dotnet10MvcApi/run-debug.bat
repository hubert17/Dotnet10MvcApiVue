@echo off
setlocal enabledelayedexpansion

REM Set Development environment so appsettings.Development.json is loaded
set "ASPNETCORE_ENVIRONMENT=Development"

REM Set paths relative to batch script directory (current folder)
set "PROJECT_DIR=%~dp0"

REM Check for Agent Mode argument
if "%~1"=="--agent" (
    REM Agent Mode: skip UAC-requiring cert trust (sandboxed terminals cannot surface elevation dialogs).
    REM Use http profile to avoid HTTPS cert issues in headless/non-interactive environments.
    echo [Agent Mode] Launching application on http://localhost:5071 (low verbosity^)...
    dotnet run --project "%PROJECT_DIR%." --launch-profile http --verbosity quiet
) else if "%~1"=="/agent" (
    echo [Agent Mode] Launching application on http://localhost:5071 (low verbosity^)...
    dotnet run --project "%PROJECT_DIR%." --launch-profile http --verbosity quiet
) else (
    REM User Mode: attempt HTTPS dev cert trust. A UAC dialog may appear - click Yes to trust the cert.
    echo [Pre-flight] Checking HTTPS developer certificate...
    dotnet dev-certs https --check --trust >nul 2>&1
    if %errorlevel% neq 0 (
        echo [Pre-flight] Certificate not trusted. A Windows security dialog will appear - please click Yes.
        dotnet dev-certs https --trust
    )
    echo [User Mode] Launching application on https://localhost:7031...
    dotnet run --project "%PROJECT_DIR%." --launch-profile https
)
