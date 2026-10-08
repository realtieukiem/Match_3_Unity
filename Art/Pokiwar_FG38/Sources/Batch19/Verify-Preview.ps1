param(
    [string]$KernelPath = (Join-Path $env:USERPROFILE '.codex/skills/art-2d/scripts/sprite-kernels.cs')
)
$ErrorActionPreference = 'Stop'
$artRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$definitions = Get-Content -Raw -Encoding UTF8 -LiteralPath "$PSScriptRoot/definitions.json" | ConvertFrom-Json
Add-Type -Path $KernelPath -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class Batch19Audit {
    public static int[] Alpha(byte[] p, int w, int h, bool card) {
        int edges = 0, holes = 0, minimum = 255;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int a = p[(y * w + x) * 4 + 3];
            minimum = Math.Min(minimum, a);
            if ((x == 0 || y == 0 || x == w - 1 || y == h - 1) && a != 0) edges++;
            if (card && x > w * .12 && x < w * .88 && y > h * .12 && y < h * .88 && a < 254) holes++;
        }
        return new[] { edges, holes, minimum };
    }
}
'@
$rows = @()
foreach ($definition in $definitions) {
    $path = Join-Path "$artRoot/Assets" $definition.file
    $image = [Art2D.Img]::Load($path)
    $card = $definition.file.StartsWith('Cards/')
    $stats = [Batch19Audit]::Alpha($image.P, $image.W, $image.H, $card)
    if ($image.W -ne $definition.width -or $image.H -ne $definition.height) { throw "Incorrect dimensions: $path" }
    if ($definition.transparent -and ($stats[0] -ne 0 -or $stats[1] -ne 0)) { throw "Incorrect sprite alpha: $path" }
    if (-not $definition.transparent -and $stats[2] -ne 255) { throw "Background has transparent pixels: $path" }
    $rows += [ordered]@{
        file = $definition.file; dimensions = @($image.W, $image.H)
        transparent = [bool]$definition.transparent; borderPixels = $stats[0]
        cardInteriorHoles = $stats[1]; minimumAlpha = $stats[2]
        sha256 = (Get-FileHash -LiteralPath $path).Hash; status = 'pass'
    }
}
$rows | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath "$PSScriptRoot/final-qc.json"
New-Item -ItemType Directory -Path "$artRoot/Previews" -Force | Out-Null
$canvas = [Drawing.Bitmap]::new(512, 934)
$graphics = [Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([Drawing.Color]::FromArgb(43, 48, 58))
$graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font = [Drawing.Font]::new('Arial', 12)
$brush = [Drawing.SolidBrush]::new([Drawing.Color]::Ivory)
function Draw-Asset([string]$File, [int]$X, [int]$Y, [int]$Width, [int]$Height) {
    $bitmap = [Drawing.Image]::FromFile((Join-Path "$artRoot/Assets" $File))
    try { $graphics.DrawImage($bitmap, $X, $Y, $Width, $Height) } finally { $bitmap.Dispose() }
}
$graphics.DrawString('Batch 19 - working and small UI scale', $font, $brush, 10, 6)
Draw-Asset 'Cards/Faces/face_mana_potion_v02.png' 20 30 176 225
Draw-Asset 'Cards/Faces/face_fire_bolt_v02.png' 216 30 176 225
Draw-Asset 'Battle/QTE/qte_button_dir_v01.png' 402 66 96 96
Draw-Asset 'Cards/Faces/face_mana_potion_v02.png' 20 273 100 128
Draw-Asset 'Cards/Faces/face_fire_bolt_v02.png' 136 273 100 128
Draw-Asset 'Battle/QTE/qte_button_dir_v01.png' 257 290 64 64
Draw-Asset 'Battle/QTE/qte_button_strike_v01.png' 349 277 128 64
Draw-Asset 'Battle/QTE/qte_button_strike_v01.png' 257 352 240 120
Draw-Asset 'Battle/Background/battle_bg_eastsea_v02.png' 0 484 512 288
$graphics.DrawString('Standing top crops (left / right)', $font, $brush, 10, 780)
$background = [Drawing.Image]::FromFile("$artRoot/Assets/Battle/Background/battle_bg_eastsea_v02.png")
try {
    $graphics.DrawImage($background, [Drawing.Rectangle]::new(10, 808, 240, 120), [Drawing.Rectangle]::new(50, 790, 600, 300), [Drawing.GraphicsUnit]::Pixel)
    $graphics.DrawImage($background, [Drawing.Rectangle]::new(262, 808, 240, 120), [Drawing.Rectangle]::new(1400, 790, 600, 300), [Drawing.GraphicsUnit]::Pixel)
} finally { $background.Dispose() }
$canvas.Save("$artRoot/Previews/Cards_QTE_reef_batch19_contact_v01.png", [Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$canvas.Dispose()
$font.Dispose()
$brush.Dispose()
Write-Output "QC: $($rows.Count)/5 exact frames; sprite borders clear; card interiors opaque; background alpha255. Contact saved."
