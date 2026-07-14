$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "MySpawnerController"
$seBin64   = "D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64"
$pluginsDir = Join-Path $seBin64 "Plugins"
$configXml  = Join-Path $pluginsDir "config.xml"
$dllName    = "MySpawnerController.dll"
$dllPath    = Join-Path $pluginsDir $dllName

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " MySpawnerController - Build and Deploy" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Validate paths ---
if (-not (Test-Path $projectDir)) {
    Write-Error "Project dir not found: $projectDir"
    exit 1
}
if (-not (Test-Path $seBin64)) {
    Write-Error "SE Bin64 not found: $seBin64"
    exit 1
}
if (-not (Test-Path $pluginsDir)) {
    Write-Error "Plugins dir not found: $pluginsDir"
    exit 1
}
if (-not (Test-Path $configXml)) {
    Write-Error "config.xml not found: $configXml"
    exit 1
}

# --- 1. Find MSBuild ---
Write-Host "[1/5] Locating MSBuild..." -ForegroundColor Yellow

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
    if (Test-Path $path) {
        $msbuild = $path
        Write-Host "  Found: $msbuild" -ForegroundColor Green
        break
    }
}

# Fallback: vswhere
if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($vsPath) {
            $candidate = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $candidate) { $msbuild = $candidate }
            else {
                $candidate = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe"
                if (Test-Path $candidate) { $msbuild = $candidate }
            }
        }
    }
}

if (-not $msbuild -or -not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found. Install Visual Studio 2022 with '.NET desktop development' workload."
    exit 1
}

# --- 2. Build ---
Write-Host "[2/5] Building Release..." -ForegroundColor Yellow
$csproj = Join-Path $projectDir "MySpawnerController.csproj"

$result = & $msbuild $csproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED" -ForegroundColor Red
    Write-Host ($result -join "`n")
    exit 1
}
Write-Host "  Build OK" -ForegroundColor Green
Write-Host ""

# --- 3. Copy DLL ---
Write-Host "[3/5] Copying DLL..." -ForegroundColor Yellow

$releaseDir = Join-Path $projectDir "bin\Release"
$builtDll = Get-ChildItem -Path $releaseDir -Recurse -Filter $dllName -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $builtDll) {
    Write-Error "Built DLL not found in $releaseDir"
    exit 1
}

# If game is running (DLL locked), kill it first
$seProc = Get-Process -Name "SpaceEngineers" -ErrorAction SilentlyContinue
if ($seProc) {
    Write-Host "  Game is running - stopping..." -ForegroundColor Magenta
    $seProc | Stop-Process -Force
    Start-Sleep -Seconds 2
}

Copy-Item -Path $builtDll.FullName -Destination $dllPath -Force
Write-Host "  $($builtDll.FullName) -> $dllPath" -ForegroundColor Green
Write-Host ""

# --- 4. Update config.xml ---
Write-Host "[4/5] Updating PluginLoader config.xml..." -ForegroundColor Yellow

[xml]$config = Get-Content $configXml -Encoding UTF8

$pluginConfig = $config.PluginConfig
if (-not $pluginConfig) {
    Write-Error "Invalid config.xml: no <PluginConfig> root"
    exit 1
}

$pluginsNode = $pluginConfig.Plugins
if (-not $pluginsNode) {
    $pluginsNode = $config.CreateElement("Plugins")
    $pluginConfig.AppendChild($pluginsNode) | Out-Null
}

$alreadyExists = $false
foreach ($idNode in $pluginsNode.Id) {
    if ($idNode.'#text' -eq $dllPath) {
        $alreadyExists = $true
        break
    }
}

if (-not $alreadyExists) {
    $newId = $config.CreateElement("Id")
    $newId.InnerText = $dllPath
    $pluginsNode.AppendChild($newId) | Out-Null
    $config.Save($configXml)
    Write-Host "  Added: $dllPath" -ForegroundColor Green
}
else {
    Write-Host "  Already registered - skipping" -ForegroundColor Gray
}
Write-Host ""

# --- 5. Summary ---
Write-Host "[5/5] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "  Plugin : $dllPath"
Write-Host "  Config : $configXml"
Write-Host "  Log    : `$env:APPDATA\SpaceEngineers\MySpawnerController.log"
Write-Host ""
Write-Host "  Launch : .\run_world.bat" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
