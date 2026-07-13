# Generates the multi-resolution tray icon used by the notify-icon (system tray).
# Run once from Windows PowerShell: pwsh ./scripts/generate-tray-icon.ps1
# Unlike the MSIX placeholder PNGs, this file is tracked in git so `dotnet run`
# shows a real tray icon without any packaging setup.

Add-Type -AssemblyName System.Drawing

function New-IconFrame([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $brush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 80, 140, 240))
    $brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 240, 100, 160))
    $pad = $size * 0.08
    $tileW = ($size - $pad * 3) / 2
    $tileH = $size - $pad * 2
    $g.FillRectangle($brush1, $pad, $pad, $tileW, $tileH)
    $g.FillRectangle($brush2, $pad * 2 + $tileW, $pad, $tileW, $tileH)

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 32, 48, 256)
$frames = $sizes | ForEach-Object { New-IconFrame $_ }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outPath = Join-Path $scriptDir '..\SwitchDesktops\Assets\tray.ico'

# Build a Vista-style ICO container: ICONDIR + ICONDIRENTRY[] + PNG-encoded frames.
$pngBytesList = $frames | ForEach-Object {
    $ms = New-Object System.IO.MemoryStream
    $_.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    ,$ms.ToArray()
}

$fs = [System.IO.File]::Open($outPath, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR
$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type = icon
$bw.Write([UInt16]$sizes.Count)

$headerSize = 6 + (16 * $sizes.Count)
$offset = $headerSize
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([Byte]$dim)          # width (0 = 256)
    $bw.Write([Byte]$dim)          # height
    $bw.Write([Byte]0)             # color palette
    $bw.Write([Byte]0)             # reserved
    $bw.Write([UInt16]1)           # color planes
    $bw.Write([UInt16]32)          # bits per pixel
    $bw.Write([UInt32]$pngBytesList[$i].Length)
    $bw.Write([UInt32]$offset)
    $offset += $pngBytesList[$i].Length
}
foreach ($b in $pngBytesList) { $bw.Write($b) }

$bw.Flush(); $bw.Dispose(); $fs.Dispose()
$frames | ForEach-Object { $_.Dispose() }

Write-Host "Wrote $outPath ($($sizes -join ', ') px frames)"
