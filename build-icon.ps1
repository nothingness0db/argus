Add-Type -AssemblyName System.Drawing

$src = Join-Path $PSScriptRoot 'Assets\logo-src.png'
$out = Join-Path $PSScriptRoot 'Assets\app.ico'
$sizes = @(16, 24, 32, 48, 64, 128, 256)

$source = [System.Drawing.Image]::FromFile($src)

$pngStreams = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($source, 0, 0, $s, $s)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngStreams += ,@{ Size = $s; Bytes = $ms.ToArray() }
    $ms.Dispose()
}
$source.Dispose()

$fs = [System.IO.File]::Open($out, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter $fs

$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$pngStreams.Count)

$dataOffset = 6 + (16 * $pngStreams.Count)
foreach ($entry in $pngStreams) {
    $size = $entry.Size
    $w = if ($size -ge 256) { 0 } else { $size }
    $h = if ($size -ge 256) { 0 } else { $size }
    $bw.Write([Byte]$w)
    $bw.Write([Byte]$h)
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$entry.Bytes.Length)
    $bw.Write([UInt32]$dataOffset)
    $dataOffset += $entry.Bytes.Length
}

foreach ($entry in $pngStreams) {
    $bw.Write($entry.Bytes)
}

$bw.Close()
$fs.Close()

Write-Host "Wrote $out ($(($pngStreams | Measure-Object).Count) sizes: $($sizes -join ', '))"
