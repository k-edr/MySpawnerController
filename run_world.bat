@echo off
REM Kill any running SE instance first (required for -world to work)
taskkill /f /im SpaceEngineers.exe >nul 2>&1
taskkill /f /im SpaceEngineersLauncher.exe >nul 2>&1
timeout /t 2 /nobreak >nul

REM Launch via Launcher (PluginLoader needs it) directly into the world
REM NOTE: world name must NOT be quoted — SE parses quotes literally
start "" "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\SpaceEngineersLauncher.exe" -world Empty_World_In -skipintro
