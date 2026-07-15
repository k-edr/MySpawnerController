<#
.SYNOPSIS
    Spawn PB grid, upload script, run with tree-size argument.
.PARAMETER Size
    Christmas tree height (default 5, 1-25).
.PARAMETER Port
    API port (default 9997).
.EXAMPLE
    .\pb_tree.ps1
    .\pb_tree.ps1 -Size 10
#>
param(
    [int]$Size = 5,
    [int]$Port = 9997
)

$ErrorActionPreference = 'Stop'
$api = "http://localhost:$Port"

# ── Script to upload ──
$code = @'
IMyTextPanel _lcd;

public Program()
{
    _lcd = GridTerminalSystem.GetBlockWithName("Inset LCD Panel") as IMyTextPanel;
    if (_lcd != null) {
        _lcd.ContentType = ContentType.TEXT_AND_IMAGE;
        _lcd.FontSize = 0.65f;
    }
    Runtime.UpdateFrequency = UpdateFrequency.None;
}

public void Main(string argument, UpdateType updateSource)
{
    int size = 5;
    if (!string.IsNullOrEmpty(argument))
        int.TryParse(argument.Trim(), out size);
    if (size < 1) size = 1;
    if (size > 25) size = 25;
    
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("[Hello World!]");
    sb.AppendLine();
    
    for (int i = 0; i < size; i++)
    {
        sb.Append(new string(' ', size - i - 1));
        sb.AppendLine(new string('*', 2 * i + 1));
    }
    int trunk = size / 3;
    if (trunk < 1) trunk = 1;
    if (trunk % 2 == 0) trunk++;
    int pad = size - trunk / 2 - 1;
    for (int i = 0; i < 2; i++)
    {
        sb.Append(new string(' ', pad));
        sb.AppendLine(new string('|', trunk));
    }
    
    string output = sb.ToString();
    if (_lcd != null) _lcd.WriteText(output);
    Echo(output);
}
'@

# ── Step 1: Spawn ──
Write-Host "[1/4] Spawning PB_Test..." -ForegroundColor Yellow
$spawn = Invoke-RestMethod "$api/api/v1/spawn" -Method Post -Body '{"blueprint":"TestGrid_PBWithPanel","displayName":"PB_Tree"}' -ContentType 'application/json'
$gridId = $spawn.grids[0].id
Write-Host "  Grid: $gridId" -ForegroundColor Green

# ── Step 2: Upload code ──
Write-Host "[2/4] Uploading script..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Web
$escaped = [System.Web.HttpUtility]::JavaScriptStringEncode($code)
$body = '{"code":"' + $escaped + '"}'
$prog = Invoke-RestMethod "$api/api/v1/grids/$gridId/blocks/0/0/0/program" -Method Put -Body $body -ContentType 'application/json'
Write-Host "  Code uploaded: $($prog.length) chars" -ForegroundColor Green

# ── Step 3: Run with argument ──
Write-Host "[3/4] Running with Size=$Size..." -ForegroundColor Yellow
$run = Invoke-RestMethod "$api/api/v1/grids/$gridId/blocks/0/0/0/run" -Method Post -Body "{`"argument`":`"$Size`"}" -ContentType 'application/json'
Write-Host "  Run: $($run.success)" -ForegroundColor Green

# ── Done ──
Write-Host "[4/4] Done!" -ForegroundColor Cyan
Write-Host "  Grid ID : $gridId"
Write-Host "  Tree    : $Size rows"
Write-Host "  LCD     : should show Hello World! + tree"
