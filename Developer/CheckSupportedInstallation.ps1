# PowerShell 5.1-compatible. Read-only compatibility check.
function Assert-XmwSupportedInstallation {
    param(
        [Parameter(Mandatory=$true)][string]$GameRoot,
        [Parameter(Mandatory=$true)][string]$ManifestPath
    )
    $ulInfo = Join-Path $GameRoot 'Mods\UndeadLegacy\ModInfo.xml'
    if (!(Test-Path -LiteralPath $ulInfo -PathType Leaf)) {
        throw 'Undead Legacy ModInfo.xml was not found. Choose the installed UL game directory.'
    }
    # The shipped ModInfo.xml is stale in the user's verified 2.7.40 package.
    # Read it for diagnostics only; never edit it or use it as the sole gate.
    $xmlVersion = 'unavailable'
    try {
        $xml = New-Object System.Xml.XmlDocument
        $xml.XmlResolver = $null
        $xml.Load($ulInfo)
        $versionNode = $xml.SelectSingleNode('/xml/Version/@value')
        if ($null -ne $versionNode) { $xmlVersion = $versionNode.Value }
    } catch {
        Write-Host 'NOTE: ModInfo.xml could not be parsed; verifying the actual DLLs instead.'
    }
    Write-Host "ModInfo.xml version (informational only): $xmlVersion"
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.schema -ne 1 -or @($manifest.checks).Count -ne 2) {
        throw 'Invalid supported-installation manifest. Re-extract the complete fixed ZIP.'
    }
    foreach ($check in $manifest.checks) {
        # These are package-owned relative paths, never paths from a game file.
        $relative = ([string]$check.path).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if ([System.IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)') {
            throw 'Unsafe path in supported-installation manifest.'
        }
        if ([string]$check.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
            throw 'Invalid reference hash in supported-installation manifest.'
        }
        $path = Join-Path $GameRoot $relative
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required game DLL is missing: $path. Current mod remains unchanged."
        }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ($actual -ne [string]$check.sha256) {
            throw ("Game DLL does not match the supplied {0} build.`nFile: {1}`nSHA256: {2}`nCurrent mod remains unchanged. Do not remove this check or change the game's ModInfo.xml." -f $check.label,$path,$actual)
        }
        Write-Host ("VERIFIED: {0} (actual DLL SHA256 matched)" -f $check.label) -ForegroundColor Green
    }
    Write-Host 'Compatibility check passed. No game-version metadata was changed.'
}
