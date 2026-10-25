@echo off
REM ============================================================
REM  ErpMobile - Build latest & run
REM  [1] Windows    [2] Android
REM  If the build fails, it auto-cleans obj and retries once.
REM ============================================================
cd /d D:\ErpMobile

set "PROJ=D:\ErpMobile\ErpMobile\ErpMobile.csproj"
set "WINFW=net10.0-windows10.0.19041.0"
set "ANDFW=net10.0-android"
set "WINEXE=D:\ErpMobile\ErpMobile\bin\Debug\net10.0-windows10.0.19041.0\win-x64\ErpMobile.exe"

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
dotnet build %PROJ% -f %WINFW% -c Debug -v q
if errorlevel 1 goto WINRETRY
goto WINRUN

:WINRETRY
echo.
echo   Build failed - cleaning temporary files and retrying...
rmdir /s /q "D:\ErpMobile\ErpMobile\obj"
dotnet build %PROJ% -f %WINFW% -c Debug -v q
if errorlevel 1 goto FAIL

:WINRUN
echo.
echo   Starting app...
start "" "%WINEXE%"
goto OK

:ANDROID
echo.
echo ============================================
echo   Preparing adb connection...
echo ============================================
where adb >nul 2>&1
if errorlevel 1 goto NOADB

adb kill-server >nul 2>&1
ping -n 2 127.0.0.1 >nul
adb start-server >nul 2>&1

REM Wait until adb can talk to a device (up to ~20 seconds)
set /a DEVTRY=0
:WAITDEV
adb shell echo ok >nul 2>&1
if not errorlevel 1 goto DEVOK
set /a DEVTRY+=1
if %DEVTRY% geq 20 goto NODEV
ping -n 2 127.0.0.1 >nul
goto WAITDEV

:NOADB
echo.
echo ============================================
echo   ERROR - adb not found on PATH
echo ============================================
echo   Install Android platform-tools, then retry.
echo.
pause
goto END

:NODEV
echo.
echo ============================================
echo   ERROR - no phone detected
echo ============================================
echo   - Connect the phone with a USB cable
echo   - Unlock the phone screen
echo   - Tap ALLOW on the USB debugging prompt
echo   - Then run this script again and choose 2
echo.
pause
goto END

:DEVOK
echo   Phone found - building...
echo.
echo ============================================
echo   Building and running (Android)...
echo ============================================
dotnet build %PROJ% -f %ANDFW% -c Debug -t:Run -v q
if errorlevel 1 goto ANDRETRY
goto OK

:ANDRETRY
echo.
echo   Build failed - cleaning temporary files and retrying...
rmdir /s /q "D:\ErpMobile\ErpMobile\obj"
adb kill-server >nul 2>&1
adb start-server >nul 2>&1
dotnet build %PROJ% -f %ANDFW% -c Debug -t:Run -v q
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
