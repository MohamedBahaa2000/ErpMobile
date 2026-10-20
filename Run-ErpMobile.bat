@echo off
REM ============================================================
REM  ErpMobile - Build latest & run
REM  [1] Windows    [2] Android
REM  If the build fails, it auto-cleans obj and retries once.
REM ============================================================
cd /d D:\ErpMobile

echo.
echo ============================================
echo          Select ERP - Run Project
echo ============================================
echo.
echo    [1] Windows
echo    [2] Android
echo.
choice /c 12 /n /m "    Choose 1-2: "
if errorlevel 2 goto ANDROID
if errorlevel 1 goto WINDOWS
goto WINDOWS

set "PROJ=D:\ErpMobile\ErpMobile\ErpMobile.csproj"
set "WINFW=net10.0-windows10.0.19041.0"
set "ANDFW=net10.0-android"

:WINDOWS
echo.
echo ============================================
echo   Stopping previous instance...
echo ============================================
taskkill /f /im ErpMobile.exe >nul 2>&1
ping -n 2 127.0.0.1 >nul

echo.
echo ============================================
echo   Building (Windows)...
echo ============================================
dotnet build ErpMobile\ErpMobile.csproj -f net10.0-windows10.0.19041.0 -c Debug -v q
if errorlevel 1 goto WINRETRY
goto WINRUN

:WINRETRY
echo.
echo   Build failed - cleaning temporary files and retrying...
rmdir /s /q "D:\ErpMobile\ErpMobile\obj"
dotnet build ErpMobile\ErpMobile.csproj -f net10.0-windows10.0.19041.0 -c Debug -v q
if errorlevel 1 goto FAIL

:WINRUN
echo.
echo   Starting app...
start "" "D:\ErpMobile\ErpMobile\bin\Debug\net10.0-windows10.0.19041.0\win-x64\ErpMobile.exe"
goto OK

:ANDROID
echo.
echo ============================================
echo   Building and running (Android)...
echo   Make sure the phone is connected via USB
echo ============================================
dotnet build ErpMobile\ErpMobile.csproj -f net10.0-android -c Debug -t:Run -v q
if errorlevel 1 goto FAIL
goto OK

:OK
echo.
echo ============================================
echo   Done - app is starting...
echo ============================================
ping -n 4 127.0.0.1 >nul
goto END

:FAIL
echo.
echo ============================================
echo   ERROR - check the messages above
echo ============================================
pause
goto END

:END
