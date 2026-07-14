$logDir = "$env:APPDATA\SpaceEngineers"
Write-Host "=== Recent logs (last 2 hours) ==="
Get-ChildItem $logDir -Filter "SpaceEngineers*.log" | 
    Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-2) } |
    Sort-Object LastWriteTime -Descending |
    Select-Object Name, LastWriteTime, Length

Write-Host ""
Write-Host "=== All .log files ==="
Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object Name, LastWriteTime, Length -First 10

Write-Host ""
Write-Host "=== MySpawnerController log (full) ==="
$myLog = Join-Path $logDir "MySpawnerController.log"
if (Test-Path $myLog) { Get-Content $myLog -Tail 20 }
