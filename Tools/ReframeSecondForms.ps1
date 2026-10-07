param(
    [string]$Source = 'D:\Project\Pokiwar_Art_FG38\Assets\Battle\Characters',
    [string]$Dest = (Join-Path $PSScriptRoot '..\Assets\Pokiwar\Art\FG38'),
    [double]$Grow = 1.10
)

Add-Type -AssemblyName System.Drawing

$forms = @(
    @{ Base = 'enemy_beetle_idle_01'; Src = 'enemy_beetle_form02'; Out = 'char_dunewing_evolved' },
    @{ Base = 'enemy_psyling_idle_01'; Src = 'enemy_psyling_form02'; Out = 'char_psyling_evolved' }
)
$poses = 'idle', 'attack', 'hit', 'defeat'

function Get-Subject([string]$path) {
    $bmp = New-Object System.Drawing.Bitmap $path
    $w = $bmp.Width; $h = $bmp.Height
    $data = $bmp.LockBits((New-Object System.Drawing.Rectangle 0, 0, $w, $h), 'ReadOnly', 'Format32bppArgb')
    $stride = $data.Stride
    $buf = New-Object byte[] ($stride * $h)
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $buf.Length)
    $bmp.UnlockBits($data); $bmp.Dispose()
    $minX = $w; $maxX = -1; $minY = $h; $maxY = -1
    for ($y = 0; $y -lt $h; $y += 2) {
        $row = $y * $stride + 3
        for ($x = 0; $x -lt $w; $x += 2) {
            if ($buf[$row + $x * 4] -gt 24) {
                if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    [pscustomobject]@{ W = $w; H = $h; MinX = $minX; MaxX = $maxX; MinY = $minY; MaxY = $maxY }
}

foreach ($f in $forms) {
    $base = Get-Subject (Join-Path $Source ($f.Base + '.png'))
    $baseHeight = $base.MaxY - $base.MinY
    $baseGap = $base.H - $base.MaxY
    $baseCentre = ($base.MinX + $base.MaxX) / 2.0

    $subjects = @{}
    foreach ($p in $poses) { $subjects[$p] = Get-Subject (Join-Path $Source ($f.Src + '_' + $p + '_01.png')) }
    $idle = $subjects['idle']
    $scale = $Grow * $baseHeight / ($idle.MaxY - $idle.MinY)

    $minX = ($subjects.Values | ForEach-Object { $_.MinX } | Measure-Object -Minimum).Minimum
    $maxX = ($subjects.Values | ForEach-Object { $_.MaxX } | Measure-Object -Maximum).Maximum
    $centre = ($minX + $maxX) / 2.0
    $ground = $base.H - $baseGap
    $used = @()

    foreach ($p in $poses) {
        $sub = $subjects[$p]
        $half = [Math]::Max($centre - $sub.MinX, $sub.MaxX - $centre)
        $poseScale = [Math]::Min($scale, [Math]::Min((($base.W - 8) / (2.0 * $half)), (($ground - 4) / ($idle.MaxY - $sub.MinY))))
        $used += ('{0} x{1:N2}' -f $p, $poseScale)
        $src = New-Object System.Drawing.Bitmap (Join-Path $Source ($f.Src + '_' + $p + '_01.png'))
        $out = New-Object System.Drawing.Bitmap $base.W, $base.H, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($out)
        $g.InterpolationMode = 'HighQualityBicubic'
        $g.PixelOffsetMode = 'HighQuality'
        $g.CompositingQuality = 'HighQuality'
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($src, [single]($base.W / 2.0 - $centre * $poseScale), [single]($ground - $idle.MaxY * $poseScale), [single]($src.Width * $poseScale), [single]($src.Height * $poseScale))
        $g.Dispose(); $src.Dispose()
        $name = if ($p -eq 'idle') { $f.Out } else { $f.Out + '_' + $p }
        $out.Save((Join-Path $Dest ($name + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        $out.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
        $out.Save((Join-Path $Dest ($name + '_right.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        $out.Dispose()
    }
    $after = Get-Subject (Join-Path $Dest ($f.Out + '.png'))
    '{0}: first form {1} px tall, second form {2} px tall (x{3:N2} of the first); {4}' -f $f.Out, $baseHeight, ($after.MaxY - $after.MinY), (($after.MaxY - $after.MinY) / $baseHeight), ($used -join ', ')
}
