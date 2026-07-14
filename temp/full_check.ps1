$logDir = "$env:APPDATA\SpaceEngineers"

# Check game process
$proc = Get-Process -Name "SpaceEngineers*" -ErrorAction SilentlyContinue
Write-Host "Process: $($proc.Count -gt 0)"

# Latest game log
$latestLog = Get-ChildItem "$logDir\SpaceEngineers_*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Latest log: $($latestLog.Name) ($($latestLog.Length) bytes)"
Write-Host ""

# Check for errors in latest log
if ($latestLog.Length -gt 0) {
    Write-Host "=== Errors ==="
    Select-String -Path $latestLog.FullName -Pattern "error|exception|access.*denied" -CaseSensitive:$false | Select-Object -Last 5
}

# Plugin log
$myLog = Join-Path $logDir "MySpawnerController.log"
Write-Host ""
Write-Host "=== Plugin log (last 10) ==="
Get-Content $myLog -Tail 10
