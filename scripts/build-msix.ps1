# Builds and signs an MSIX package locally.
# Run from Windows PowerShell after generating placeholder assets and the self-signed cert.

param(
    [string]$Configuration = 'Release',
    [string]$Version = '1.0.0.0',
    [string]$OutputDir = "$PSScriptRoot\..\deploy"
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\.."
$project = Join-Path $repo 'SwitchDesktops\SwitchDesktops.csproj'

Write-Host "Building MSIX (version $Version, $Configuration)..."

dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PackageMsix=true `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageDir="$OutputDir\" `
    -p:UapAppxPackageBuildMode=SideloadOnly `
    -p:AppxBundle=Never `
    -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateKeyFile="$repo\SwitchDesktops\SwitchDesktops_TemporaryKey.pfx" `
    -p:Version=$Version

Write-Host ""
Write-Host "MSIX output: $OutputDir"
Get-ChildItem $OutputDir -Filter *.msix | ForEach-Object { Write-Host "  $($_.Name)" }
