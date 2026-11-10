param([string]$GameRoot='')
$ErrorActionPreference='Stop'
$defaultRoot='C:\game\steam\steamapps\common\7 Days To Die'
if([string]::IsNullOrWhiteSpace($GameRoot)){$GameRoot=Read-Host "Game directory (Enter = $defaultRoot)";if([string]::IsNullOrWhiteSpace($GameRoot)){$GameRoot=$defaultRoot}}
$GameRoot=(Resolve-Path -LiteralPath $GameRoot.Trim().Trim('"')).Path
if(Get-Process -Name '7DaysToDie','7DaysToDieServer' -ErrorAction SilentlyContinue){throw 'Close the game/server first.'}
if(!(Test-Path -LiteralPath (Join-Path $GameRoot '7DaysToDie.exe'))){throw 'Not a game directory.'}
$target=Join-Path $GameRoot 'Mods\zXMW_ULCategoryBackpack'
$receipt=Get-Content -LiteralPath (Join-Path $target 'InstallReceipt.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($receipt.version -ne '0.3.4-rc1' -or !$receipt.hadPreviousMod){throw 'No matching previous installation was recorded. No files changed.'}
$root=(Resolve-Path -LiteralPath (Join-Path $GameRoot 'XMW_CategoryBackpack_Backups')).Path
$backup=(Resolve-Path -LiteralPath $receipt.previousModBackup).Path
if(![string]::Equals((Split-Path -Parent $backup),$root,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected backup location; no files changed.'}
if(!(Test-Path -LiteralPath (Join-Path $backup 'ModInfo.xml'))){throw 'Recorded backup incomplete; no files changed.'}
$disabled=Join-Path $root ('disabled-v034-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
Move-Item -LiteralPath $target -Destination $disabled
try{Move-Item -LiteralPath $backup -Destination $target}
catch{if(!(Test-Path -LiteralPath $target)){Move-Item -LiteralPath $disabled -Destination $target};throw}
Write-Host 'RESTORED previous category backpack. Current experimental files were preserved in backup.' -ForegroundColor Green
Write-Host "Preserved: $disabled"
