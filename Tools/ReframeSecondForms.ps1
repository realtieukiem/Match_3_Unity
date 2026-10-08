param(
    [string]$Source = (Join-Path $PSScriptRoot '..\Art\Pokiwar_FG38\Assets\Battle\Characters'),
    [string]$Dest = (Join-Path $PSScriptRoot '..\Assets\Pokiwar\Art\FG38'),
    [double]$Grow = 1.10,
    [int]$CanvasWidth = 1402,
    [int]$CanvasHeight = 1122,
    [int]$GroundGap = 86,
    [double]$FirstFormScale = 1.49,
    [double]$SecondFormScale = 1.277
)

Add-Type -AssemblyName System.Drawing

$forms = @(
    @{ Base = 'enemy_beetle_idle_01'; Src = 'enemy_beetle_form02'; Out = 'char_dunewing_evolved' },
    @{ Base = 'enemy_psyling_idle_01'; Src = 'enemy_psyling_form02'; Out = 'char_psyling_evolved' },
    @{ Scale = $FirstFormScale; Src = 'enemy_samgong'; Out = 'char_samgong' },
    @{ Scale = $FirstFormScale; Src = 'enemy_bebeboom'; Out = 'char_bebeboom' },
    @{ Scale = $FirstFormScale; Src = 'enemy_ngoclam'; Out = 'char_ngoclam' },
    @{ Scale = $FirstFormScale; Src = 'enemy_doimora'; Out = 'char_doimora' },
    @{ Scale = $FirstFormScale; Src = 'enemy_voirong'; Out = 'char_voirong' },
    @{ Scale = $FirstFormScale; Src = 'boss_ongnamhai_form01'; Out = 'char_ongnamhai' },
    @{ Scale = $SecondFormScale; Src = 'enemy_ngoclam_form02'; Out = 'char_ngoclam_evolved' },
    @{ Scale = $SecondFormScale; Src = 'enemy_doimora_form02'; Out = 'char_doimora_evolved' },
    @{ Scale = $SecondFormScale; Src = 'enemy_voirong_form02'; Out = 'char_voirong_evolved' },
    @{ Scale = $SecondFormScale; Src = 'boss_ongnamhai_form02'; Out = 'char_ongnamhai_evolved' }
)
$poses = 'idle', 'attack', 'hit', 'defeat'
$extraIdle = 2, 3

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
    $subjects = @{}
    $files = @{}
    foreach ($p in $poses) { $files[$p] = $f.Src + '_' + $p + '_01.png' }
    foreach ($n in $extraIdle) { $extra = $f.Src + '_idle_0' + $n + '.png'; if (Test-Path (Join-Path $Source $extra)) { $files['idle' + $n] = $extra } }
    foreach ($p in $files.Keys) { $subjects[$p] = Get-Subject (Join-Path $Source $files[$p]) }
    $idle = $subjects['idle']

    $width = $CanvasWidth; $height = $CanvasHeight; $gap = $GroundGap
    if ($f.Base) {
        $base = Get-Subject (Join-Path $Source ($f.Base + '.png'))
        $width = $base.W; $height = $base.H; $gap = $base.H - $base.MaxY
        $scale = $Grow * ($base.MaxY - $base.MinY) / ($idle.MaxY - $idle.MinY)
    }
    else { $scale = $f.Scale }

    $minX = ($subjects.Values | ForEach-Object { $_.MinX } | Measure-Object -Minimum).Minimum
    $maxX = ($subjects.Values | ForEach-Object { $_.MaxX } | Measure-Object -Maximum).Maximum
    $centre = ($minX + $maxX) / 2.0
    $ground = $height - $gap
    $used = @()

    foreach ($p in $files.Keys) {
        $sub = $subjects[$p]
        $half = [Math]::Max($centre - $sub.MinX, $sub.MaxX - $centre)
        $poseScale = [Math]::Min($scale, [Math]::Min((($width - 8) / (2.0 * $half)), (($ground - 4) / ($idle.MaxY - $sub.MinY))))
        $used += ('{0} x{1:N2}' -f $p, $poseScale)
        $src = New-Object System.Drawing.Bitmap (Join-Path $Source $files[$p])
        $out = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($out)
        $g.InterpolationMode = 'HighQualityBicubic'
        $g.PixelOffsetMode = 'HighQuality'
        $g.CompositingQuality = 'HighQuality'
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($src, [single]($width / 2.0 - $centre * $poseScale), [single]($ground - $idle.MaxY * $poseScale), [single]($src.Width * $poseScale), [single]($src.Height * $poseScale))
        $g.Dispose(); $src.Dispose()
        $name = if ($p -eq 'idle') { $f.Out } else { $f.Out + '_' + $p }
        $out.Save((Join-Path $Dest ($name + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        $out.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
        $out.Save((Join-Path $Dest ($name + '_right.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        $out.Dispose()
    }

    $after = Get-Subject (Join-Path $Dest ($f.Out + '.png'))
    '{0}: idle {1}x{2} px on {3}x{4}; {5}' -f $f.Out, ($after.MaxX - $after.MinX), ($after.MaxY - $after.MinY), $width, $height, ($used -join ', ')
}
