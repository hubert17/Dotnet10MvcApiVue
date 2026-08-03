@echo off
:: ============================================================
:: devcert-setup.bat - One-time HTTPS Developer Certificate Setup
:: Run this ONCE on a new machine after cloning the repository.
:: A Windows UAC elevation dialog will appear - click YES to trust
:: the localhost certificate in the Windows Root CA store.
:: ============================================================

:: Self-elevate to Administrator if not already elevated
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [Setup] Requesting administrator elevation for certificate trust...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo =============================================================
echo   ASP.NET Core HTTPS Developer Certificate Setup
echo =============================================================
echo.

echo [Step 1] Removing any existing stale localhost certificates...
dotnet dev-certs https --clean
echo.

echo [Step 2] Generating and trusting new HTTPS developer certificate...
echo          A Windows security dialog may appear - click YES to trust.
dotnet dev-certs https --trust
echo.

echo [Step 3] Verifying trusted certificate...
dotnet dev-certs https --check --trust
if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] HTTPS developer certificate is valid and trusted!
    echo           You can now run the app with: .\run-debug.bat
) else (
    echo.
    echo [WARNING] Certificate verification failed. 
    echo           Try running this script again, or check Windows Certificate Manager.
)

echo.
pause
