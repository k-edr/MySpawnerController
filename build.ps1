$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$apiProject     = Join-Path $scriptDir "MySpawnerController.Api"
$mainProject    = Join-Path $scriptDir "MySpawnerController"
$swaggerProject = Join-Path $scriptDir "MySpawnerController.Swagger"
$seBin64        = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64"
$pluginsDir     = Join-Path $seBin64 "Plugins"
$configXml      = Join-Path $pluginsDir "config.xml"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " MySpawnerController - Build and Deploy" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Validate paths ---
if (-not (Test-Path $mainProject))    { Write-Error "Main project dir not found: $mainProject"; exit 1 }
if (-not (Test-Path $apiProject))     { Write-Error "API project dir not found: $apiProject"; exit 1 }
if (-not (Test-Path $swaggerProject)) { Write-Error "Swagger project dir not found: $swaggerProject"; exit 1 }
if (-not (Test-Path $seBin64))        { Write-Error "SE Bin64 not found: $seBin64"; exit 1 }
if (-not (Test-Path $pluginsDir))     { Write-Error "Plugins dir not found: $pluginsDir"; exit 1 }
if (-not (Test-Path $configXml))      { Write-Error "config.xml not found: $configXml"; exit 1 }

# --- Kill game if running ---
$seProc = Get-Process -Name "SpaceEngineers" -ErrorAction SilentlyContinue
$launcherProc = Get-Process -Name "SpaceEngineersLauncher" -ErrorAction SilentlyContinue
if ($seProc -or $launcherProc) {
    Write-Host "[0] Game is running - stopping..." -ForegroundColor Magenta
    & cmd /c "taskkill /f /im SpaceEngineers.exe 2>nul" 2>$null
    & cmd /c "taskkill /f /im SpaceEngineersLauncher.exe 2>nul" 2>$null
    Start-Sleep -Seconds 3
}

# --- 1. Find MSBuild ---
Write-Host "[1/8] Locating MSBuild..." -ForegroundColor Yellow

$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) { $msbuild = $path; Write-Host "  Found: $msbuild" -ForegroundColor Green; break }
}

if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($vsPath) {
            $candidate = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
            if (-not (Test-Path $candidate)) { $candidate = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe" }
            if (Test-Path $candidate) { $msbuild = $candidate }
        }
    }
}

if (-not $msbuild -or -not (Test-Path $msbuild)) { Write-Error "MSBuild not found."; exit 1 }

# --- 2. Build API project (netstandard2.0) ---
Write-Host "[2/8] Building MySpawnerController.Api..." -ForegroundColor Yellow
$apiCsproj = Join-Path $apiProject "MySpawnerController.Api.csproj"

$result = & $msbuild $apiCsproj /p:Configuration=Release /t:Restore /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "API RESTORE FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }

$result = & $msbuild $apiCsproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "API BUILD FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }
Write-Host "  API Build OK" -ForegroundColor Green

# --- 3. Build main project (.NET Framework 4.8) ---
Write-Host "[3/8] Building MySpawnerController..." -ForegroundColor Yellow
$mainCsproj = Join-Path $mainProject "MySpawnerController.csproj"

$result = & $msbuild $mainCsproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "MAIN BUILD FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }
Write-Host "  Main Build OK" -ForegroundColor Green

# --- 4. Build Swagger project (net8.0 console app) ---
Write-Host "[4/8] Building MySpawnerController.Swagger..." -ForegroundColor Yellow
$swaggerCsproj = Join-Path $swaggerProject "MySpawnerController.Swagger.csproj"

$result = & $msbuild $swaggerCsproj /p:Configuration=Release /t:Restore /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "SWAGGER RESTORE FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }

$result = & $msbuild $swaggerCsproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "SWAGGER BUILD FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }
Write-Host "  Swagger Build OK" -ForegroundColor Green
Write-Host ""

# --- 5. Copy DLLs ---
Write-Host "[5/8] Copying DLLs..." -ForegroundColor Yellow

# Main plugin DLL (with retry)
$mainDllPath = Join-Path $pluginsDir "MySpawnerController.dll"
$mainDll = Get-ChildItem -Path (Join-Path $mainProject "bin\Release") -Recurse -Filter "MySpawnerController.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $mainDll) { Write-Error "Main DLL not found"; exit 1 }

$copied = $false
for ($i = 0; $i -lt 5; $i++) {
    try { Copy-Item -Path $mainDll.FullName -Destination $mainDllPath -Force -ErrorAction Stop; $copied = $true; break }
    catch { Write-Host "  Retry $($i+1)/5..." -ForegroundColor DarkYellow; Start-Sleep -Seconds 2 }
}
if (-not $copied) { Write-Error "Cannot copy main DLL - file locked."; exit 1 }
Write-Host "  MySpawnerController.dll" -ForegroundColor Green

# API DLL
$apiOut = Join-Path $apiProject "bin\Release\netstandard2.0"
$apiDll = Join-Path $apiOut "MySpawnerController.Api.dll"
if (Test-Path $apiDll) {
    Copy-Item -Path $apiDll -Destination (Join-Path $pluginsDir "MySpawnerController.Api.dll") -Force
    Write-Host "  MySpawnerController.Api.dll" -ForegroundColor Green
}

# System.Text.Json + deps
$nugetDeps = @("System.Text.Json.dll", "System.Text.Encodings.Web.dll",
    "System.Runtime.CompilerServices.Unsafe.dll", "System.Threading.Tasks.Extensions.dll",
    "System.Memory.dll", "System.Buffers.dll", "System.Numerics.Vectors.dll", "System.ValueTuple.dll")
foreach ($dep in $nugetDeps) {
    $depPath = Join-Path $apiOut $dep
    if (Test-Path $depPath) {
        Copy-Item -Path $depPath -Destination (Join-Path $pluginsDir $dep) -Force
        Write-Host "  $dep" -ForegroundColor Gray
    }
}

# Swagger exe (publish as self-contained folder)
$swaggerOut = Join-Path $swaggerProject "bin\Release\net8.0"
$swaggerDest = Join-Path $pluginsDir "Swagger"
if (-not (Test-Path $swaggerDest)) { New-Item -ItemType Directory -Path $swaggerDest -Force | Out-Null }

$swaggerExe = Join-Path $swaggerOut "MySpawnerController.Swagger.exe"
if (Test-Path $swaggerExe) {
    Copy-Item -Path $swaggerExe -Destination $swaggerDest -Force
    Write-Host "  MySpawnerController.Swagger.exe" -ForegroundColor Green
    # Copy runtime deps too
    Get-ChildItem -Path $swaggerOut -Filter "*.dll" | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $swaggerDest -Force
    }
    Get-ChildItem -Path $swaggerOut -Filter "*.json" | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $swaggerDest -Force
    }
}

Write-Host ""

# --- 6. Update config.xml ---
Write-Host "[6/8] Updating PluginLoader config.xml..." -ForegroundColor Yellow

[xml]$config = Get-Content $configXml -Encoding UTF8

$pluginConfig = $config.PluginConfig
if (-not $pluginConfig) { Write-Error 'Invalid config.xml: no PluginConfig root'; exit 1 }

$pluginsNode = $pluginConfig.Plugins
if (-not $pluginsNode) {
    $pluginsNode = $config.CreateElement("Plugins")
    $pluginConfig.AppendChild($pluginsNode) | Out-Null
}

$dllPath = Join-Path $pluginsDir "MySpawnerController.dll"
$alreadyExists = $false
foreach ($idNode in $pluginsNode.Id) {
    if ($idNode.'#text' -eq $dllPath) { $alreadyExists = $true; break }
}

if (-not $alreadyExists) {
    $newId = $config.CreateElement("Id")
    $newId.InnerText = $dllPath
    $pluginsNode.AppendChild($newId) | Out-Null
    $config.Save($configXml)
    Write-Host "  Added: $dllPath" -ForegroundColor Green
} else {
    Write-Host "  Already registered - skipping" -ForegroundColor Gray
}
Write-Host ""

# --- 7. Summary ---
Write-Host "[7/8] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "  Plugins dir : $pluginsDir"
Write-Host "  Config      : $configXml"
Write-Host "  Log         : `$env:APPDATA\SpaceEngineers\MySpawnerController.log"
Write-Host ""
Write-Host "  Launch game     : .\run_world.bat" -ForegroundColor Cyan
Write-Host "  Launch swagger  : .\run_swagger.bat" -ForegroundColor Cyan
Write-Host "  Game API        : http://localhost:9997" -ForegroundColor Cyan
Write-Host "  Swagger UI      : http://localhost:9998/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
