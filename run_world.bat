@echo off
REM Kill any running SE instance first (required for -world to work)
taskkill /f /im SpaceEngineers.exe >nul 2>&1
taskkill /f /im SpaceEngineersLauncher.exe >nul 2>&1
timeout /t 2 /nobreak >nul

REM Launch Space Engineers directly into the specified world
REM NOTE: -world argument must NOT be quoted (SE parses it literally)
cd /d D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64
start SpaceEngineers.exe -world Empty_World_In -skipintro
