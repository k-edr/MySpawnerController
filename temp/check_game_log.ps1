$log = "$env:APPDATA\SpaceEngineers\SpaceEngineers_20260715_001220569.log"
Write-Host "=== Last 50 lines of $log ==="
Get-Content $log -Tail 50

Write-Host ""
Write-Host "=== Searching for errors/warnings ==="
Select-String -Path $log -Pattern "error|exception|crash|fail|missing" -CaseSensitive:$false | Select-Object -Last 10
