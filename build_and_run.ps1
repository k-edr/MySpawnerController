$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Build + Run Game + Run Swagger + Spawn" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Build
Write-Host "[1/5] Building..." -ForegroundColor Yellow
& "$scriptDir\build.ps1"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host ""

# 2. Kill old + launch game
Write-Host "[2/5] Launching game..." -ForegroundColor Yellow
& cmd /c "taskkill /f /im SpaceEngineers.exe 2>nul"
& cmd /c "taskkill /f /im SpaceEngineersLauncher.exe 2>nul"
Start-Sleep -Seconds 2

$seBin64 = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64"
if (-not (Test-Path "$seBin64\SpaceEngineers.exe")) { Write-Error "SpaceEngineers.exe not found: $seBin64"; exit 1 }
Start-Process -FilePath "$seBin64\SpaceEngineers.exe" -WorkingDirectory $seBin64 -ArgumentList '-world', 'Empty_World_In', '-skipintro'

# 3. Launch Swagger in separate terminal
Write-Host "[3/5] Launching Swagger UI..." -ForegroundColor Yellow
$swaggerExe = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\Plugins\Swagger\GridSpawner.Swagger.exe"
if (Test-Path $swaggerExe) {
    Start-Process -FilePath $swaggerExe
    Write-Host "  Swagger: http://localhost:9998/swagger" -ForegroundColor Cyan
} else {
    Write-Host "  Swagger exe not found (build first)" -ForegroundColor DarkYellow
}
Write-Host ""

# 4. Wait for world
Write-Host "[4/5] Waiting for world load (polling game on port 9997)..." -ForegroundColor Yellow

$ready = $false
for ($i = 0; $i -lt 90; $i++) {
    try {
        $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 http://localhost:9997/api/v1/health 2>$null
        if ($resp.Content -match '"ready"\s*:\s*true') { $ready = $true; break }
    } catch { }
    Write-Host "  ." -NoNewline
    Start-Sleep -Seconds 2
}
Write-Host ""

if (-not $ready) {
    Write-Host "TIMEOUT: world not ready after 3 min" -ForegroundColor Red
    Write-Host "  Try manually: Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn-tests"
    exit 1
}
Write-Host "  World loaded!" -ForegroundColor Green
Write-Host ""

# 5. Spawn
Write-Host "[5/5] Spawning test grids..." -ForegroundColor Yellow
try {
    $spawn = Invoke-WebRequest -UseBasicParsing -Method POST -TimeoutSec 30 http://localhost:9997/api/v1/spawn-tests
    $json = $spawn.Content | ConvertFrom-Json
    Write-Host "  Spawned $($json.grids.Count) grid(s):" -ForegroundColor Green
    foreach ($g in $json.grids) {
        Write-Host "    $($g.name) Id=$($g.id) @ X:$($g.position.x) Y:$($g.position.y) Z:$($g.position.z)"
    }
} catch {
    Write-Host "SPAWN FAILED: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Game API  : http://localhost:9997" -ForegroundColor Cyan
Write-Host " Swagger   : http://localhost:9998/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
