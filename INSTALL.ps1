param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
$installLog = Join-Path $PSScriptRoot 'INSTALL_LAST.log'
$transcriptStarted = $false

function Invoke-XmwLoggedNative {
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [Parameter(Mandatory=$true)][string]$LogPath
    )
    if (!(Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        throw "Build tool not found: $FilePath"
    }
    $previousPreference = $ErrorActionPreference
    $outputLines = @()
    $exitCode = -1
    try {
        # Native stderr must be captured, not converted into an early terminating
        # PowerShell NativeCommandError before the compiler messages can be logged.
        $ErrorActionPreference = 'Continue'
        $outputLines = @(& $FilePath @ArgumentList 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    foreach ($entry in $outputLines) {
        $text = [string]$entry
        Write-Host $text
        $text | Out-File -LiteralPath $LogPath -Append -Encoding utf8
    }
    $summary = "Native exit code: $exitCode"
    Write-Host $summary
    $summary | Out-File -LiteralPath $LogPath -Append -Encoding utf8
    return [int]$exitCode
}

try {
    Start-Transcript -LiteralPath $installLog -Force -ErrorAction Stop | Out-Null
    $transcriptStarted = $true
    Write-Host 'XMW Category Backpack v0.3.4-rc1 / Use-based taxonomy'
    Write-Host 'Local installation only. No downloads, no Google Drive access.'
    Write-Host "Full installer output: $installLog"
$defaultRoot = 'C:\game\steam\steamapps\common\7 Days To Die'
if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $GameRoot = Read-Host "Game directory (Enter = $defaultRoot)"
    if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = $defaultRoot }
}
$GameRoot = (Resolve-Path -LiteralPath $GameRoot.Trim().Trim('"')).Path
if (Get-Process -Name '7DaysToDie','7DaysToDieServer' -ErrorAction SilentlyContinue) {
    throw 'Close the game/server before upgrading. No installed mod was changed.'
}
if (!(Test-Path -LiteralPath (Join-Path $GameRoot '7DaysToDie.exe'))) { throw 'Game executable not found.' }
foreach ($line in Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([0-9a-f]{64})  (.+)$') { throw 'Invalid checksum manifest.' }
    $expected=$Matches[1]; $rel=$Matches[2]
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $PSScriptRoot $rel)).Hash -ne $expected) {
        throw "Package checksum mismatch: $rel"
    }
}
# Verify the actual, user-supplied DLLs rather than the stale ModInfo.xml label.
. (Join-Path $PSScriptRoot 'Developer\CheckSupportedInstallation.ps1')
Assert-XmwSupportedInstallation -GameRoot $GameRoot -ManifestPath (Join-Path $PSScriptRoot 'Developer\SupportedInstallation.json')
$compiler = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (!$compiler) { throw 'Windows C# compiler unavailable. Current mod remains unchanged.' }
$managed = Join-Path $GameRoot '7DaysToDie_Data\Managed'
$harmony = Join-Path $GameRoot 'BepInEx\core\0Harmony.dll'
$gameAssembly = Join-Path $managed 'Assembly-CSharp.dll'
if (!(Test-Path -LiteralPath $harmony) -or !(Test-Path -LiteralPath $gameAssembly)) { throw 'Required game references missing.' }
$name='zXMW_ULCategoryBackpack'
$target=Join-Path $GameRoot "Mods\$name"
$backupRoot=Join-Path $GameRoot 'XMW_CategoryBackpack_Backups'
$stamp=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-v034-'+[guid]::NewGuid().ToString('N').Substring(0,8)
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$work=Join-Path $backupRoot ('build-'+$stamp)
$stage=Join-Path $work $name
New-Item -ItemType Directory -Path $work -Force | Out-Null
$log=Join-Path $backupRoot ('Compile-'+$stamp+'.log')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $stage -Recurse
$source=Join-Path $stage 'Scripts\ULCategoryBackpack.cs'
$testExe=Join-Path $stage 'XMW_ContractTests.exe'
$testSource=Join-Path $PSScriptRoot 'Developer\contract_tests.cs'
Write-Host 'Compiling and running isolated contract tests (not a game test)...'
# Isolated tests use Windows .NET Framework references ONLY, not Unity's netstandard.
# /noconfig suppresses csc.rsp. Explicitly include System.dll for HashSet<T>'s ISet<T>.
$frameworkDir = Split-Path -Parent $compiler
$testRefs = @('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object {
    $refPath = Join-Path $frameworkDir $_
    if (!(Test-Path -LiteralPath $refPath)) { throw "Windows test reference missing: $refPath. Current mod unchanged." }
    $refPath
}
Write-Host "Test compiler: $compiler"
foreach ($refPath in $testRefs) { Write-Host "Test reference: $refPath" }
$argsTest=@('/nologo','/noconfig','/nostdlib+','/target:exe','/optimize+','/langversion:5','/codepage:65001',"/out:$testExe") + @($testRefs | ForEach-Object { '/r:'+$_ }) + @($source,$testSource)
$code = Invoke-XmwLoggedNative -FilePath $compiler -ArgumentList $argsTest -LogPath $log
if ($code -ne 0 -or !(Test-Path -LiteralPath $testExe)) { throw "Test compilation failed (exit $code). Current mod unchanged. Send INSTALL_LAST.log beside INSTALL.cmd." }
$code = Invoke-XmwLoggedNative -FilePath $testExe -ArgumentList @() -LogPath $log
if ($code -ne 0) { throw "Contract tests failed (exit $code). Current mod unchanged. Send INSTALL_LAST.log beside INSTALL.cmd." }
$dll=Join-Path $stage 'XMW_ULCategoryBackpack.dll'
$common=@('/nologo','/noconfig','/target:library','/optimize+',"/out:$dll", "/r:$gameAssembly", "/r:$harmony")
Write-Host 'Building the actual plugin against this installation...'
# First attempt uses the game's managed BCL, avoiding the previous netstandard mismatch.
$refs=@('netstandard.dll','mscorlib.dll','System.dll','System.Core.dll','UnityEngine.dll','UnityEngine.CoreModule.dll')
$refs=@($refs | ForEach-Object { Join-Path $managed $_ } | Where-Object { Test-Path -LiteralPath $_ })
$profiles=@()
if (Test-Path -LiteralPath (Join-Path $managed 'netstandard.dll')) {
    $profiles+=,@($common+@('/nostdlib+')+@($refs | ForEach-Object { '/r:'+$_ })+@($source))
}
# Fallback for installations with framework-compatible game references.
$profiles+=,@($common+@($refs | ForEach-Object { '/r:'+$_ })+@($source))
$ok=$false
foreach ($profile in $profiles) {
    $code = Invoke-XmwLoggedNative -FilePath $compiler -ArgumentList $profile -LogPath $log
    if ($code -eq 0 -and (Test-Path -LiteralPath $dll)) { $ok=$true; break }
}
if (!$ok) { throw "Plugin compilation failed. Current mod unchanged. Send INSTALL_LAST.log beside INSTALL.cmd. Build log: $log" }
[System.Reflection.AssemblyName]::GetAssemblyName($dll) | Out-Null
# Migrate prior optional overrides without silently reusing changed numeric meanings.
$oldOverride=Join-Path $target 'Config\CategoryOverrides.tsv'
$migration=@{10=21;11=20;12=22;13=22;14=24;15=25;16=26;17=9}
$migrated=0
if (Test-Path -LiteralPath $oldOverride) {
    $lines=New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('# XMW taxonomy v034-use; exact overrides preserved, legacy leaf codes migrated.')
    foreach($line in Get-Content -LiteralPath $oldOverride -Encoding UTF8) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
        $a=$line.Split([char]9)
        $n=0
        if ($a.Length -ne 2 -or [string]::IsNullOrWhiteSpace($a[0]) -or ![int]::TryParse($a[1],[ref]$n)) {
            throw 'Invalid existing CategoryOverrides.tsv. Current mod unchanged.'
        }
        if ($migration.ContainsKey($n)) { $n=$migration[$n]; $migrated++ }
        if (!(($n -ge 1 -and $n -le 9) -or ($n -ge 20 -and $n -le 27))) {
            throw 'Unsupported override category ID. Current mod unchanged.'
        }
        $lines.Add($a[0].Trim()+[char]9+$n)
    }
    [System.IO.File]::WriteAllLines((Join-Path $stage 'Config\CategoryOverrides.tsv'),$lines,(New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Preserved existing overrides; legacy leaf codes migrated: $migrated. Original file stays in backup."
}
$code = Invoke-XmwLoggedNative -FilePath $testExe -ArgumentList @('--validate-config') -LogPath $log
if ($code -ne 0) { throw 'Merged config validation failed. Current mod unchanged.' }
Remove-Item -LiteralPath $testExe -Force
$testLog=Join-Path $stage 'XMW_Diagnostics.log'
if (Test-Path -LiteralPath $testLog) { Remove-Item -LiteralPath $testLog -Force }
$backup=Join-Path $backupRoot $stamp
$hadOld=Test-Path -LiteralPath $target
$receipt=@{version='0.3.4-rc1';gameRoot=$GameRoot;previousModBackup=$backup;hadPreviousMod=[bool]$hadOld;installedAt=(Get-Date -Format o);legacyOverrideRowsMigrated=$migrated}
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'InstallReceipt.json') -Encoding UTF8

if ($hadOld) { Move-Item -LiteralPath $target -Destination $backup }
try { Move-Item -LiteralPath $stage -Destination $target }
catch {
    if ($hadOld -and !(Test-Path -LiteralPath $target)) { Move-Item -LiteralPath $backup -Destination $target }
    throw
}
Write-Host 'INSTALLED v0.3.4-rc1. Compiler and mock tests passed.' -ForegroundColor Green
Write-Host 'GAME TEST STILL REQUIRED: category contents, tooltips, hover, wheel, drag and Smart Network.'
Write-Host "Installed: $target"
Write-Host "Previous mod backup: $backup"
Write-Host "Build log: $log"
Write-Host 'Materials -> Ore+Ingots / Basic Stock / Industrial Parts / Vehicle Parts / Chemicals / Building / Farming / Energy.'

}
catch {
    Write-Host ''
    Write-Host ('INSTALLATION FAILED: '+$_.Exception.Message) -ForegroundColor Red
    Write-Host 'The compiler/test details are printed above, not only the summary.'
    Write-Host "Send this one file: $installLog" -ForegroundColor Yellow
    Write-Host 'Do not copy staging files into Mods manually or bypass the tests.'
    throw
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
