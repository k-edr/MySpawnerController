Write-Host "Testing /api/v1/openapi.json..."
$p = Start-Process -FilePath "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins\Swagger\MySpawnerController.Swagger.exe" -PassThru
Start-Sleep -Seconds 1

try {
    $r = Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 http://localhost:9998/api/v1/openapi.json
    Write-Host "STATUS: $($r.StatusCode)" -ForegroundColor Green
    Write-Host $r.Content.Substring(0, [Math]::Min(300, $r.Content.Length))
} catch {
    Write-Host "FAIL" -ForegroundColor Red
}

Stop-Process -Id $p.Id -Force
