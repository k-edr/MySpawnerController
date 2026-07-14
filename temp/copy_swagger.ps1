$src = "D:\repos\SE.Plugins\MySpawnerController\MySpawnerController.Swagger\bin\Release\net8.0\MySpawnerController.Swagger.exe"
$dst = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins\Swagger\MySpawnerController.Swagger.exe"
Copy-Item $src $dst -Force
Write-Host "Copied OK"
