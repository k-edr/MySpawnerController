$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$seDir = Join-Path $appData "SpaceEngineers"
$logFile = Join-Path $seDir "SpaceEngineers.log"
$logFile2 = Join-Path $seDir "SpaceEngineers.cfg"

Write-Host "=== Looking for logs in: $seDir ==="

if (Test-Path $logFile) {
    Write-Host "`n[SpaceEngineers.log - last 30 lines]"
    Get-Content $logFile -Tail 30
} else {
    Write-Host "SpaceEngineers.log not found"
    # Search for any .log files
    Get-ChildItem $seDir -Filter "*.log" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 5 | ForEach-Object {
        Write-Host "`nFound: $($_.FullName)"
        Get-Content $_.FullName -Tail 10
    }
}

Write-Host "`n=== Saves directory ==="
$savesDir = Join-Path $seDir "Saves"
if (Test-Path $savesDir) {
    Get-ChildItem $savesDir -Directory -Recurse -Depth 2 | Select-Object FullName
} else {
    Write-Host "Saves dir not found"
}
