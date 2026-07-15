@echo off
REM Kill old instances + launch Space Engineers.
REM AutoWorldLoader plugin reads GridSpawner.json "autoLoadWorld" and loads the world.

taskkill /f /im SpaceEngineers.exe >nul 2>&1
taskkill /f /im SpaceEngineersLauncher.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo Starting Space Engineers...
start "" "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\SpaceEngineersLauncher.exe"
echo World auto-load configured in: %APPDATA%\SpaceEngineers\GridSpawner.json
