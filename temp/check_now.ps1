$log = "$env:APPDATA\SpaceEngineers\MySpawnerController.log"
Write-Host "=== Plugin log ==="
Get-Content $log -Tail 30
Write-Host ""

Write-Host "=== Testing port 9997 ==="
try {
    $r = Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 http://localhost:9997/api/v1/health
    Write-Host "RESPONSE: $($r.Content)" -ForegroundColor Green
} catch {
    Write-Host "NOT REACHABLE: $($_.Exception.Message)" -ForegroundColor Red
}
