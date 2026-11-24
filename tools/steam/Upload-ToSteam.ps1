# Upload Builds/Windows to the main Steam app and the Playtest app in one login.
# DryRun writes the VDFs and does not download SteamCMD or ask for a password.
param(
    [switch]$DryRun,
    [ValidateSet('main', 'playtest')]
    [string]$Only
)

$ErrorActionPreference = 'Stop'

function Stop-Upload {
    param([string]$Message)
    Write-Host $Message
    exit 1
}

function Get-GitShortHash {
    param([string]$RepoRoot)
    try {
        $hash = & git -C $RepoRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $hash) {
            return ("$hash").Trim()
        }
    } catch {
    }
    Write-Warning "git is unavailable. The build description uses unknown in place of the hash."
    return 'unknown'
}

function Write-Utf8 {
    param([string]$Path, [string]$Text)
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Text, $utf8)
}

function New-DepotVdf {
    param([string]$DepotId, [string]$ContentRoot)
    $exclusions = @(
        '*.pdb',
        '*_BurstDebugInformation_DoNotShip',
        '*_BurstDebugInformation_DoNotShip/*',
        '*_BackUpThisFolder_ButDontShipItWithYourGame',
        '*_BackUpThisFolder_ButDontShipItWithYourGame/*',
        '*_ButDontShipItWithYourGame*',
        '*_DoNotShip*',
        'steam_appid.txt'
    )
    $lines = @(
        '"DepotBuildConfig"',
        '{',
        "`t`"DepotID`" `"$DepotId`"",
        "`t`"ContentRoot`" `"$ContentRoot`"",
        "`t`"FileMapping`"",
        "`t{",
        "`t`t`"LocalPath`" `"*`"",
        "`t`t`"DepotPath`" `".`"",
        "`t`t`"Recursive`" `"1`"",
        "`t}"
    )
    foreach ($pattern in $exclusions) {
        $lines += "`t`"FileExclusion`" `"$pattern`""
    }
    $lines += '}'
    return ($lines -join "`r`n") + "`r`n"
}

function New-AppVdf {
    param(
        [string]$AppId,
        [string]$Description,
        [string]$ContentRoot,
        [string]$BuildOutput,
        [string]$DepotId,
        [string]$DepotVdf
    )
    $lines = @(
        '"AppBuild"',
        '{',
        "`t`"AppID`" `"$AppId`"",
        "`t`"Desc`" `"$Description`"",
        "`t`"ContentRoot`" `"$ContentRoot`"",
        "`t`"BuildOutput`" `"$BuildOutput`"",
        "`t`"SetLive`" `"`"",
        "`t`"Depots`"",
        "`t{",
        "`t`t`"$DepotId`" `"$DepotVdf`"",
        "`t}",
        '}'
    )
    return ($lines -join "`r`n") + "`r`n"
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$configPath = Join-Path $PSScriptRoot 'steam_upload.json'
if (-not (Test-Path -LiteralPath $configPath)) {
    Stop-Upload "Missing $configPath."
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$buildFolder = "$($config.buildFolder)"
if ([string]::IsNullOrWhiteSpace($buildFolder)) {
    Stop-Upload "steam_upload.json has no buildFolder."
}
if (-not [System.IO.Path]::IsPathRooted($buildFolder)) {
    $buildFolder = Join-Path $repoRoot $buildFolder
}
$buildFolder = [System.IO.Path]::GetFullPath($buildFolder)
$exeName = "$($config.exeName)"
$exePath = Join-Path $buildFolder $exeName

if (-not (Test-Path -LiteralPath $buildFolder -PathType Container)) {
    Stop-Upload "Build folder is missing: $buildFolder. Run Caravans/Build Windows first."
}
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    Stop-Upload "Player exe is missing: $exePath. Run Caravans/Build Windows first."
}

$steamApi = Get-ChildItem -LiteralPath $buildFolder -Recurse -Filter 'steam_api64.dll' -File -ErrorAction SilentlyContinue |
    Select-Object -First 1
if (-not $steamApi) {
    Stop-Upload "steam_api64.dll was not found under $buildFolder. Run Caravans/Build Windows first."
}

$dataConfig = Join-Path $buildFolder 'data\config'
if (-not (Test-Path -LiteralPath $dataConfig -PathType Container)) {
    Write-Warning "The player looks stale: data/config is missing. A fresh Caravans/Build Windows is needed."
    Stop-Upload "data/config is missing beside the player. Run Caravans/Build Windows first."
}

$jsonCount = @(Get-ChildItem -LiteralPath $dataConfig -Filter '*.json' -File).Count
if ($jsonCount -lt 1) {
    Stop-Upload "data/config has no JSON files. Run Caravans/Build Windows first."
}

$mapTokens = Join-Path $dataConfig 'map_tokens.json'
if (-not (Test-Path -LiteralPath $mapTokens -PathType Leaf)) {
    Write-Warning "The player looks stale: data/config/map_tokens.json is missing. A fresh Caravans/Build Windows is needed."
}

try {
    $dirty = & git -C $repoRoot status --porcelain 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "git status could not be read. Continuing."
    } elseif ($dirty) {
        Write-Warning "The working tree is dirty. Continuing."
    }
} catch {
    Write-Warning "git status could not be read. Continuing."
}

$hash = Get-GitShortHash -RepoRoot $repoRoot
$date = Get-Date -Format 'yyyy-MM-dd'
$description = "Caravans of the Cutting Sands $hash $date"

$apps = @($config.apps)
if ($Only) {
    $apps = @($apps | Where-Object { "$($_.key)" -eq $Only })
    if ($apps.Count -eq 0) {
        Stop-Upload "steam_upload.json has no app with key $Only."
    }
}

$generated = Join-Path $PSScriptRoot 'generated'
New-Item -ItemType Directory -Force -Path $generated | Out-Null

$appVdfs = @()
foreach ($app in $apps) {
    $appId = "$($app.appId)"
    $depotId = "$($app.depotId)"
    $output = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "output\$appId"))
    New-Item -ItemType Directory -Force -Path $output | Out-Null

    $depotVdf = Join-Path $generated "depot_$depotId.vdf"
    $appVdf = Join-Path $generated "app_$appId.vdf"
    Write-Utf8 -Path $depotVdf -Text (New-DepotVdf -DepotId $depotId -ContentRoot $buildFolder)
    Write-Utf8 -Path $appVdf -Text (New-AppVdf -AppId $appId -Description $description -ContentRoot $buildFolder -BuildOutput $output -DepotId $depotId -DepotVdf $depotVdf)
    Write-Host "Depot VDF: $depotVdf"
    Write-Host "App VDF:   $appVdf"
    $appVdfs += $appVdf
}

Write-Host "Build description: $description"
Write-Host "Content root: $buildFolder"

if ($DryRun) {
    Write-Host "Dry run only. SteamCMD was not downloaded or started, and no password was requested."
    exit 0
}

$steamcmd = Join-Path $PSScriptRoot 'steamcmd\steamcmd.exe'
if (-not (Test-Path -LiteralPath $steamcmd -PathType Leaf)) {
    $zip = Join-Path $env:TEMP ("steamcmd-" + [guid]::NewGuid().ToString('n') + '.zip')
    $dest = Join-Path $PSScriptRoot 'steamcmd'
    Write-Host "Downloading SteamCMD from https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    try {
        Invoke-WebRequest -Uri 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile $zip -UseBasicParsing
        if (Test-Path -LiteralPath $dest) {
            Remove-Item -LiteralPath $dest -Recurse -Force
        }
        New-Item -ItemType Directory -Path $dest | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $dest)
    } finally {
        if (Test-Path -LiteralPath $zip) {
            Remove-Item -LiteralPath $zip -Force
        }
    }
    if (-not (Test-Path -LiteralPath $steamcmd -PathType Leaf)) {
        Stop-Upload "SteamCMD did not extract to $steamcmd."
    }
}

$user = $null
$usernameFile = Join-Path $PSScriptRoot 'steam_username.txt'
if (Test-Path -LiteralPath $usernameFile) {
    foreach ($line in Get-Content -LiteralPath $usernameFile) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -gt 0) {
            $user = $trimmed
            break
        }
    }
}
if ([string]::IsNullOrWhiteSpace($user)) {
    $user = (Read-Host 'Steam username').Trim()
}
if ([string]::IsNullOrWhiteSpace($user)) {
    Stop-Upload "A Steam username is required."
}

$steamArgs = @('+login', $user)
foreach ($appVdf in $appVdfs) {
    $steamArgs += '+run_app_build'
    $steamArgs += $appVdf
}
$steamArgs += '+quit'

$printed = @("`"$steamcmd`"") + ($steamArgs | ForEach-Object {
    if ($_ -match '\s') { "`"$_`"" } else { $_ }
})
Write-Host ("SteamCMD: " + ($printed -join ' '))

Push-Location (Split-Path -Parent $steamcmd)
try {
    & $steamcmd @steamArgs
    $steamCode = $LASTEXITCODE
} finally {
    Pop-Location
}
if ($steamCode -ne 0) {
    Stop-Upload "SteamCMD exited with code $steamCode."
}

Write-Host "Upload finished. SteamCMD left SetLive empty. Set the build live on Steamworks, then Publish."
foreach ($app in $apps) {
    $label = "$($app.key)"
    $appId = "$($app.appId)"
    if ($label -eq 'playtest') {
        Write-Host "Playtest: set this build live right away: https://partner.steamgames.com/apps/builds/$appId"
    } else {
        Write-Host "Main: set the default branch live when this build is the one for review: https://partner.steamgames.com/apps/builds/$appId"
    }
}
Write-Host 'Installation > General: the launch-option executable must be exactly "Caravans of the Cutting Sands.exe", Windows, 64-bit. The install folder should be "Caravans of the Cutting Sands".'
exit 0
