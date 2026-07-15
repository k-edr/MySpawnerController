@echo off
taskkill /f /im SpaceEngineers.exe >nul 2>&1
taskkill /f /im SpaceEngineersLauncher.exe >nul 2>&1
timeout /t 2 /nobreak >nul

powershell -Command "Start-Process 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\SpaceEngineersLauncher.exe'"
echo Game launched. Load world manually: Empty_World_In
echo API: http://localhost:9997 | Swagger: http://localhost:9998/swagger
