$ErrorActionPreference = 'Stop'
$defaultRoot = 'C:\game\steam\steamapps\common\7 Days To Die'
$gameRoot = Read-Host "Game directory (Enter = $defaultRoot)"
if ([string]::IsNullOrWhiteSpace($gameRoot)) { $gameRoot = $defaultRoot }
$gameRoot = (Resolve-Path -LiteralPath $gameRoot.Trim('"')).Path
if (!(Test-Path -LiteralPath (Join-Path $gameRoot '7DaysToDie.exe'))) { throw 'Invalid game directory.' }
if (Get-Process -Name '7DaysToDie','7DaysToDieServer' -ErrorAction SilentlyContinue) { throw 'Close the game and server first.' }
$target = Join-Path $gameRoot 'Mods\zXMW_ULCategoryBackpack'
if (Test-Path -LiteralPath $target) {
    $root = Join-Path $gameRoot 'XMW_CategoryBackpack_Backups'
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    $destination = Join-Path $root ('disabled-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    Move-Item -LiteralPath $target -Destination $destination
    Write-Host "Mod disabled; files/logs preserved: $destination"
} else { Write-Host 'Category mod is already absent.' }
Write-Host 'The next launch uses the original UL backpack. Other mods and saves were untouched.'
