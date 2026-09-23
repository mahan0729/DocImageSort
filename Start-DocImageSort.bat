@echo off
title DocImageSort Launcher
echo.
echo  ================================================
echo   DocImageSort - Starting up...
echo  ================================================
echo.

:: ── Create required folders if they don't exist ───────────────────────────
if not exist "C:\DocImageSort\Drop"  mkdir "C:\DocImageSort\Drop"
if not exist "C:\DocImageSort\Files" mkdir "C:\DocImageSort\Files"
echo  [OK] Folders ready: C:\DocImageSort\Drop and C:\DocImageSort\Files

:: ── Start the API ─────────────────────────────────────────────────────────
echo  [..] Starting API...
start "DocImageSort API" /D "%~dp0DocImageSort.Api" cmd /k "dotnet run"

:: ── Wait for the API to initialize ────────────────────────────────────────
echo  [..] Waiting for API to start (10 seconds)...
timeout /t 10 /nobreak >nul

:: ── Start the frontend ────────────────────────────────────────────────────
echo  [..] Starting frontend...
start "DocImageSort Web" /D "%~dp0DocImageSort.Web" cmd /k "npm run dev"

:: ── Wait for the frontend to initialize ──────────────────────────────────
echo  [..] Waiting for frontend to start (5 seconds)...
timeout /t 5 /nobreak >nul

:: ── Open browser ──────────────────────────────────────────────────────────
echo  [OK] Opening browser...
start http://localhost:5173

echo.
echo  ================================================
echo   DocImageSort is running!
echo   Browser: http://localhost:5173
echo   API:     http://localhost:5000
echo.
echo   Drop documents into: C:\DocImageSort\Drop
echo.
echo   Close the API and Web windows to stop.
echo  ================================================
echo.
