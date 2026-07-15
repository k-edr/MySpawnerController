$root = $PSScriptRoot

$replacements = @(
    # Order matters: longer first
    @{Old='GridSpawner.Api.Application'; New='GridSpawner.Api.Application'},
    @{Old='GridSpawner.Api.Infrastructure'; New='GridSpawner.Api.Infrastructure'},
    @{Old='GridSpawner.Swagger.Application'; New='GridSpawner.Swagger.Application'},
    @{Old='GridSpawner.Swagger.Infrastructure'; New='GridSpawner.Swagger.Infrastructure'},
    @{Old='GridSpawner.Api'; New='GridSpawner.Api'},
    @{Old='GridSpawner.Shared'; New='GridSpawner.Shared'},
    @{Old='GridSpawner.Swagger'; New='GridSpawner.Swagger'},
    @{Old='GridSpawner.Plugin'; New='GridSpawner.Plugin'}
)

$extensions = @('*.cs', '*.csproj', '*.sln', '*.ps1', '*.bat', '*.md', '*.json')

$files = Get-ChildItem -Path $root -Recurse -Include $extensions | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|\.git|\.agents|temp)\\'
}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $changed = $false
    foreach ($r in $replacements) {
        if ($content.Contains($r.Old)) {
            $content = $content.Replace($r.Old, $r.New)
            $changed = $true
        }
    }
    if ($changed) {
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "  $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
