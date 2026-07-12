# Generates placeholder PNG assets for the MSIX package.
# Run once from a Windows PowerShell terminal: pwsh ./scripts/generate-placeholder-assets.ps1
# Replace with real icons before shipping.

Add-Type -AssemblyName System.Drawing

$targets = @(
    @{ File = 'StoreLogo.png';         W = 50;  H = 50  },
    @{ File = 'Square150x150Logo.png'; W = 150; H = 150 },
    @{ File = 'Square44x44Logo.png';   W = 44;  H = 44  },
    @{ File = 'Wide310x150Logo.png';   W = 310; H = 150 },
    @{ File = 'SmallTile.png';         W = 71;  H = 71  },
    @{ File = 'LargeTile.png';         W = 310; H = 310 },
    @{ File = 'SplashScreen.png';      W = 620; H = 300 }
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Join-Path $scriptDir '..\SwitchDesktops\Assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

foreach ($t in $targets) {
    $bmp = New-Object System.Drawing.Bitmap($t.W, $t.H)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::FromArgb(30, 30, 30))

    $brush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 80, 140, 240))
    $brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 240, 100, 160))
    $pad = [Math]::Min($t.W, $t.H) * 0.12
    $tileW = ($t.W - $pad * 3) / 2
    $tileH = $t.H - $pad * 2
    $g.FillRectangle($brush1, $pad, $pad, $tileW, $tileH)
    $g.FillRectangle($brush2, $pad * 2 + $tileW, $pad, $tileW, $tileH)

    $out = Join-Path $assets $t.File
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "Wrote $out ($($t.W)x$($t.H))"
}

Write-Host "Done. Replace these placeholders with real icons before shipping."
