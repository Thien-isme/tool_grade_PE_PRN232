@echo off
title Khoi dong API Runner Tool
cd /d "%~dp0"
echo Dang khoi chay he thong kiem thu API tu dong...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_tools.ps1"
echo.
echo He thong da dung. Nhan phim bat ky de thoat...
pause > nul
