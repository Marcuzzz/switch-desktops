# Builds and signs an MSIX package locally.
# Run from Windows PowerShell after generating placeholder assets and the self-signed cert.
#
# This drives makeappx.exe / signtool.exe from the Windows 10/11 SDK directly, rather than
# relying on Visual Studio's single-project MSIX packaging tooling (EnableMsixTooling), which
# `dotnet publish` alone does not provide even with the Windows SDK installed. Install just the
# SDK (winget install Microsoft.WindowsSDK.10.0.26100) to run this without Visual Studio.

param(
    [string]$Configuration = 'Release',
    [string]$Version = '1.0.0.0',
    [string]$OutputDir = "$PSScriptRoot\..\deploy",
    [string]$PfxPassword = 'devpassword'
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\.."
$project = Join-Path $repo 'SwitchDesktops\SwitchDesktops.csproj'
$pfx = Join-Path $repo 'SwitchDesktops\SwitchDesktops_TemporaryKey.pfx'

if (-not (Test-Path $pfx)) {
    throw "Signing cert not found at $pfx. Run scripts/create-self-signed-cert.ps1 first."
}

function Find-SdkTool([string]$ToolName) {
    $bin = 'C:\Program Files (x86)\Windows Kits\10\bin'
    $candidate = Get-ChildItem $bin -Directory -Filter '10.0.*' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName 'x64' } |
        Where-Object { Test-Path (Join-Path $_ $ToolName) } |
        Select-Object -First 1
    if (-not $candidate) {
        throw "$ToolName not found under $bin. Install the Windows SDK: winget install Microsoft.WindowsSDK.10.0.26100"
    }
    return Join-Path $candidate $ToolName
}

$makeAppx = Find-SdkTool 'makeappx.exe'
$signTool = Find-SdkTool 'signtool.exe'

Write-Host "Publishing app (version $Version, $Configuration)..."
$publishDir = Join-Path $repo "SwitchDesktops\bin\$Configuration\net8.0-windows10.0.19041.0\win-x64\publish"
dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:Version=$Version

Write-Host "Assembling MSIX layout..."
$layout = Join-Path $repo 'obj\msix-layout'
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
New-Item -ItemType Directory -Force -Path $layout | Out-Null

Copy-Item "$publishDir\*" $layout -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $layout 'Assets') | Out-Null
Copy-Item (Join-Path $repo 'SwitchDesktops\Assets\*.png') (Join-Path $layout 'Assets') -Force

$manifest = Get-Content (Join-Path $repo 'SwitchDesktops\Package.appxmanifest') -Raw
$manifest = $manifest -replace 'Version="1\.0\.0\.0"', "Version=""$Version"""
[System.IO.File]::WriteAllText((Join-Path $layout 'AppxManifest.xml'), $manifest, (New-Object System.Text.UTF8Encoding($false)))

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$msixPath = Join-Path $OutputDir "SwitchDesktops_${Version}_x64.msix"
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }

Write-Host "Packing MSIX..."
& $makeAppx pack /d $layout /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx.exe failed with exit code $LASTEXITCODE" }

Write-Host "Signing MSIX..."
& $signTool sign /fd SHA256 /a /f $pfx /p $PfxPassword $msixPath
if ($LASTEXITCODE -ne 0) { throw "signtool.exe failed with exit code $LASTEXITCODE" }

Write-Host ""
Write-Host "MSIX output: $OutputDir"
Get-ChildItem $OutputDir -Filter *.msix | ForEach-Object { Write-Host "  $($_.Name)" }
