Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\ThomasDavis\Desktop\Projects\Platmatics_Invoice\horizontal-dark-1080w.png'
$outIco = 'C:\Users\ThomasDavis\Desktop\Projects\Platmatics_Invoice\PlatmaticsInvoice\Assets\platmatics.ico'

$img = [System.Drawing.Bitmap]::new($src)
Write-Output "Source: $($img.Width)x$($img.Height)"

# Find the bounding box of non-white pixels in the left portion (the diamond mark)
$scanWidth = [Math]::Min([int]($img.Width * 0.3), $img.Width)
$minX = $scanWidth; $maxX = 0; $minY = $img.Height; $maxY = 0
for ($y = 0; $y -lt $img.Height; $y += 2) {
    for ($x = 0; $x -lt $scanWidth; $x += 2) {
        $p = $img.GetPixel($x, $y)
        if ($p.A -gt 40 -and ($p.R -lt 230 -or $p.G -lt 230 -or $p.B -lt 230)) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
Write-Output "Mark bounds: ($minX,$minY)-($maxX,$maxY)"

$markW = $maxX - $minX + 1
$markH = $maxY - $minY + 1
$side = [Math]::Max($markW, $markH)
$pad = [int]($side * 0.12)
$canvasSide = $side + 2 * $pad

# Compose the mark centered on a square white canvas
$square = [System.Drawing.Bitmap]::new($canvasSide, $canvasSide)
$g = [System.Drawing.Graphics]::FromImage($square)
$g.Clear([System.Drawing.Color]::Transparent)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$destX = [int](($canvasSide - $markW) / 2)
$destY = [int](($canvasSide - $markH) / 2)
$srcRect = [System.Drawing.Rectangle]::new($minX, $minY, $markW, $markH)
$destRect = [System.Drawing.Rectangle]::new($destX, $destY, $markW, $markH)
$g.DrawImage($img, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$img.Dispose()

# Render each icon size to an in-memory PNG
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngBlobs = @()
foreach ($s in $sizes) {
    $bmp = [System.Drawing.Bitmap]::new($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($square, [System.Drawing.Rectangle]::new(0, 0, $s, $s))
    $g.Dispose()
    $ms = [System.IO.MemoryStream]::new()
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngBlobs += ,@($s, $ms.ToArray())
    $ms.Dispose()
}
$square.Dispose()

# Pack PNG blobs into an ICO container
$fs = [System.IO.FileStream]::new($outIco, [System.IO.FileMode]::Create)
$bw = [System.IO.BinaryWriter]::new($fs)
$bw.Write([UInt16]0)                  # reserved
$bw.Write([UInt16]1)                  # type: icon
$bw.Write([UInt16]$pngBlobs.Count)    # image count
$offset = 6 + 16 * $pngBlobs.Count
foreach ($entry in $pngBlobs) {
    $s = $entry[0]; $data = $entry[1]
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # width (0 = 256)
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # height
    $bw.Write([Byte]0)                # palette colors
    $bw.Write([Byte]0)                # reserved
    $bw.Write([UInt16]1)              # color planes
    $bw.Write([UInt16]32)             # bits per pixel
    $bw.Write([UInt32]$data.Length)   # data size
    $bw.Write([UInt32]$offset)        # data offset
    $offset += $data.Length
}
foreach ($entry in $pngBlobs) { $bw.Write($entry[1]) }
$bw.Dispose()
$fs.Dispose()

Write-Output "Icon written: $outIco ($((Get-Item $outIco).Length) bytes)"
