$saveDir = "$env:APPDATA\SpaceEngineers\Saves\76561199017018740\Empty_World_In"
Write-Host "Checking: $saveDir"
Get-ChildItem $saveDir -Recurse | Select-Object FullName, Length
Write-Host ""
Write-Host "--- Looking for recent game log ---"
$logDir = "$env:APPDATA\SpaceEngineers"
Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object Name, LastWriteTime, Length
