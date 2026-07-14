Write-Host "Starting Swagger test..."
$p = Start-Process -FilePath "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins\Swagger\MySpawnerController.Swagger.exe" -PassThru
Start-Sleep -Seconds 2

try {
    $r = Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 http://localhost:9998/swagger
    Write-Host "STATUS: $($r.StatusCode) LENGTH: $($r.Content.Length)" -ForegroundColor Green
    Write-Host "FIRST 200 CHARS:"
    Write-Host $r.Content.Substring(0, [Math]::Min(200, $r.Content.Length))
} catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
}

Stop-Process -Id $p.Id -Force
