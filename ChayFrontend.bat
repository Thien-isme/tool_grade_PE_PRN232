@echo off
title API Runner - Frontend
cd /d "%~dp0frontend"
where npm.cmd >nul 2>&1
if errorlevel 1 (
    echo [LOI] Chua cai Node.js. Tai tai: https://nodejs.org
    pause
    exit /b 1
)
if not exist "node_modules\" (
    echo Dang npm install...
    call npm.cmd install
)
echo Mo http://localhost:5173 trong trinh duyet...
start http://localhost:5173
call npm.cmd run dev
pause
