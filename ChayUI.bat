@echo off
title API Runner - Giao dien React (UI)
cd /d "%~dp0frontend"
where npm.cmd >nul 2>&1
if errorlevel 1 (
    echo [LOI] Chua cai Node.js: https://nodejs.org
    pause
    exit /b 1
)
if not exist "node_modules\" (
    echo Dang npm install...
    call npm.cmd install
)
echo.
echo Backend can chay tai http://localhost:5155
echo UI se mo tai http://localhost:5173
echo (Giu cua so nay mo - dung Ctrl+C de tat Vite)
echo.
start http://localhost:5173
call npm.cmd run dev
pause
