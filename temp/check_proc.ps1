$proc = Get-Process -Name "SpaceEngineers*" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Game IS running:" -ForegroundColor Green
    $proc | Select-Object ProcessName, Id, StartTime
} else {
    Write-Host "Game NOT running" -ForegroundColor Red
}
