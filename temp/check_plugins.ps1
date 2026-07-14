$swaggerDir = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins\Swagger"
Write-Host "=== Swagger dir contents ==="
Get-ChildItem $swaggerDir -ErrorAction SilentlyContinue | Select-Object Name, Length
Write-Host ""
Write-Host "=== All files in Plugins dir ==="
Get-ChildItem "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins" -Filter "MySpawner*" | Select-Object Name, Length
