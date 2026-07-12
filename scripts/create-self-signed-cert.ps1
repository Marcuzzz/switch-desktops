# Creates a self-signed code-signing certificate for MSIX packaging.
# Run once, from an ELEVATED PowerShell on Windows.
#
# The Subject MUST match the <Identity Publisher="..."> in Package.appxmanifest exactly.
# If you change one, change the other.

param(
    [string]$Subject = 'CN=SwitchDesktopsDev',
    [string]$OutputDir = "$PSScriptRoot\..",
    [string]$PfxPassword = 'devpassword',
    [int]$ValidYears = 5
)

$ErrorActionPreference = 'Stop'

Write-Host "Generating self-signed cert with subject: $Subject"

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName 'SwitchDesktops Self-Signed' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

Write-Host "Thumbprint: $($cert.Thumbprint)"

$pfxPath = Join-Path $OutputDir 'SwitchDesktops\SwitchDesktops_TemporaryKey.pfx'
$cerPath = Join-Path $OutputDir 'deploy\SwitchDesktops.cer'

$securePwd = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath $pfxPath -Password $securePwd | Out-Null
Export-Certificate    -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath $cerPath | Out-Null

Write-Host ""
Write-Host "Wrote:"
Write-Host "  $pfxPath  (used to SIGN the MSIX during build)"
Write-Host "  $cerPath   (public cert users import to TRUST installs)"
Write-Host ""
Write-Host "For local test installs, import the .cer into 'Trusted People' on Local Machine:"
Write-Host "  certutil -addstore -f 'TrustedPeople' '$cerPath'"
Write-Host ""
Write-Host "Do NOT commit the .pfx file. Add SwitchDesktops_TemporaryKey.pfx to .gitignore."
