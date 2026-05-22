@echo off
title Run API Runner and Autograder Tool
cd /d "%~dp0"
echo =====================================================
echo    KHOI DONG DONG THOI CA BACKEND VA FRONTEND...
echo =====================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_tools.ps1"
echo.
echo He thong da dung. Nhan phim bat ky de thoat...
pause > nul
