$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$seBin64   = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64"
$launcher  = Join-Path $seBin64 "SpaceEngineersLauncher.exe"
$swagger   = Join-Path $seBin64 "Plugins\Swagger\GridSpawner.Swagger.exe"
$apiUrl    = "http://localhost:9997"
$apiHealth = "$apiUrl/api/v1/health"
$apiSpawn  = "$apiUrl/api/v1/spawn-tests"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " GridSpawner -- Build, Run, Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Build ──────────────────────────────────────
Write-Host "[1/5] Building all projects..." -ForegroundColor Yellow
& "$scriptDir\build.ps1"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host ""

# ── Step 2: Kill old + Launch game ─────────────────────
Write-Host "[2/5] Launching game..." -ForegroundColor Yellow
Stop-Process -Name SpaceEngineers -Force -ErrorAction SilentlyContinue
Stop-Process -Name SpaceEngineersLauncher -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

if (-not (Test-Path $launcher)) { Write-Error "Launcher not found: $launcher"; exit 1 }
Start-Process -FilePath $launcher
Write-Host "  Game launched (AutoWorldLoader will load the world)." -ForegroundColor Green

# ── Step 3: Launch Swagger ─────────────────────────────
Write-Host "[3/5] Launching Swagger UI..." -ForegroundColor Yellow
if (Test-Path $swagger) {
    Start-Process -FilePath $swagger
    Write-Host "  Swagger: http://localhost:9998/swagger" -ForegroundColor Cyan
} else {
    Write-Host "  Swagger exe not found -- build first." -ForegroundColor DarkYellow
}
Write-Host ""

# ── Step 4: Wait for world ─────────────────────────────
Write-Host "[4/5] Waiting for world (polling $apiHealth)..." -ForegroundColor Yellow
$ready = $false
for ($i = 0; $i -lt 90; $i++) {
    try {
        $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 -Uri $apiHealth
        if ($resp.Content -match '"ready"\s*:\s*true') { $ready = $true; break }
    } catch { }
    Write-Host "  ." -NoNewline
    Start-Sleep -Seconds 2
}
Write-Host ""

if (-not $ready) {
    Write-Host "TIMEOUT: world not ready after 3 min" -ForegroundColor Red
    Write-Host "  Try manually: Invoke-WebRequest -UseBasicParsing -Method POST $apiSpawn"
    exit 1
}
Write-Host "  World loaded!" -ForegroundColor Green
Write-Host ""

# ── Step 5: Spawn test grids ───────────────────────────
Write-Host "[5/5] Spawning test grids..." -ForegroundColor Yellow
try {
    $spawn = Invoke-WebRequest -UseBasicParsing -Method POST -TimeoutSec 30 -Uri $apiSpawn
    $json = $spawn.Content | ConvertFrom-Json
    Write-Host "  Spawned $($json.grids.Count) grid(s):" -ForegroundColor Green
    foreach ($g in $json.grids) {
        Write-Host "    $($g.name)  Id=$($g.id)  @ X:$($g.position.x) Y:$($g.position.y) Z:$($g.position.z)"
    }
} catch {
    Write-Host "SPAWN FAILED: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Game API  : $apiUrl" -ForegroundColor Cyan
Write-Host "  Swagger   : http://localhost:9998/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
