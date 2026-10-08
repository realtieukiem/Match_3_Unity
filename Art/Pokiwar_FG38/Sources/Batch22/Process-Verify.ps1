param([string]$KernelPath = (Join-Path $env:USERPROFILE '.codex/skills/art-2d/scripts/sprite-kernels.cs'), [switch]$ReplaceOwnedExports)
$ErrorActionPreference = 'Stop'
$artRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -Path $KernelPath -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class Batch22Pixels {
    public static int Normalize(byte[] p) {
        int n = 0;
        for (int i = 0; i < p.Length; i += 4) {
            if (p[i + 3] == 1) { Array.Clear(p, i, 4); n++; }
        }
        return n;
    }
    public static int Border(byte[] p, int w, int h) {
        int n = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            if ((x == 0 || y == 0 || x == w - 1 || y == h - 1) && p[(y * w + x) * 4 + 3] > 0) n++;
        return n;
    }
    public static int[] Registration(byte[] a, byte[] b, int w, int h) {
        int differences = 0, count = 0, maxAlphaDelta = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            if (x >= w * .22 && y <= h * .80) continue;
            int i = (y * w + x) * 4;
            int aa = a[i + 3], ba = b[i + 3];
            if ((aa >= 128) != (ba >= 128)) differences++;
            maxAlphaDelta = Math.Max(maxAlphaDelta, Math.Abs(aa - ba));
            count++;
        }
        return new[] {differences, count, maxAlphaDelta};
    }
}
'@
$definitions = Get-Content -LiteralPath "$PSScriptRoot/definitions.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$previousQc = if (Test-Path -LiteralPath "$PSScriptRoot/final-qc.json") { Get-Content -LiteralPath "$PSScriptRoot/final-qc.json" -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
$rows = @()
foreach ($definition in $definitions) {
    $target = Join-Path "$artRoot/Assets" $definition.file
    if (Test-Path -LiteralPath $target) {
        $oldRow = $previousQc.assets | Where-Object file -eq $definition.file
        if (-not $ReplaceOwnedExports -or -not $oldRow -or (Get-FileHash -LiteralPath $target).Hash -ne $oldRow.sha256) { throw "Output is not an unchanged owned export: $target" }
    }
    $raw = [Art2D.Img]::Load((Join-Path $PSScriptRoot $definition.raw))
    $key = if ($definition.matte) { [Art2D.Kit]::Key($raw, 60, 2, 40, $false, 255, 0, 255) | ConvertFrom-Json } else { $null }
    $removed = [Batch22Pixels]::Normalize($raw.P)
    $source = [Art2D.Kit]::Inspect($raw, $definition.id) | ConvertFrom-Json
    if ($source.touches_edge) { throw "Source touches canvas edge: $($definition.id)" }
    if ($definition.id -eq 'ui_button_green_v01') {
        $box = $source.content_box
        $crop = $raw.Crop($box[0], $box[1], $box[2], $box[3])
        $width = 504
        $height = 152
        $output = [Art2D.Img]::new(512, 160)
        $scaled = [Art2D.Kit]::Resample($crop, $width, $height, $false)
        $left = [int][Math]::Floor((512 - $width) / 2.0)
        $top = [int][Math]::Floor((160 - $height) / 2.0)
        if (-not $output.Blit($scaled, $left, $top)) { throw 'Button export clipped' }
    } else {
        if ($raw.W -ne 1254 -or $raw.H -ne 1254) { throw 'Checkbox alignment seed dimensions changed' }
        $output = [Art2D.Kit]::Resample($raw, 128, 128, $false)
    }
    $border = [Batch22Pixels]::Border($output.P, $output.W, $output.H)
    if ($border -ne 0) { throw "Nontransparent export border: $($definition.id)" }
    $interiorMinimum = $null
    if ($definition.id -eq 'ui_button_green_v01') {
        $interiorMinimum = 255
        for ($y = 40; $y -lt 105; $y++) { for ($x = 70; $x -lt 442; $x++) {
            $interiorMinimum = [Math]::Min($interiorMinimum, $output.P[($y * $output.W + $x) * 4 + 3])
        } }
        if ($interiorMinimum -ne 255) { throw 'Solid button face contains transparency' }
    }
    $output.Save($target)
    $row = [ordered]@{
        file = $definition.file; resolution = @($output.W, $output.H)
        sourceInspection = $source; removedAlphaOne = $removed
        outputInspection = ([Art2D.Kit]::Inspect($output, $definition.id) | ConvertFrom-Json)
        borderPixels = $border; clipped = $false
        key = $key; selectedRevision = $definition.revision; buttonInteriorMinimumAlpha = $interiorMinimum
        rawSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $definition.raw)).Hash
        sha256 = (Get-FileHash -LiteralPath $target).Hash; status = 'pass'
    }
    $rows += $row
    Write-Output "$($definition.id): $($output.W)x$($output.H), border=$border, alpha1Removed=$removed"
}
$off = [Art2D.Img]::Load("$artRoot/Assets/UI/Common/ui_checkbox_off_v01.png")
$on = [Art2D.Img]::Load("$artRoot/Assets/UI/Common/ui_checkbox_on_v01.png")
$registration = [Batch22Pixels]::Registration($off.P, $on.P, 128, 128)
$qc = [ordered]@{
    assets = $rows; checkboxRegistration = [ordered]@{
        transform = 'Identical whole-canvas 1254x1254 to 128x128 premultiplied area resample; no independent fit or recenter'
        observedRegion = 'Left 22 percent plus bottom 20 percent, outside added tick'
        alphaMaskDifferences = $registration[0]; comparedPixels = $registration[1]
        maximumAlphaDelta = $registration[2]; pixelIdenticalArtwork = $false
    }
    buttonSliceBorder = @(48, 48, 48, 48)
    buttonNormalization = 'Key opaque magenta raw, crop whole silhouette, normalize to existing blue 504x152 content frame at 4,4'
    verification = 'V1 pending visual preview'
}
[IO.File]::WriteAllText("$PSScriptRoot/final-qc.json", ($qc | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
$canvas = [Drawing.Bitmap]::new(512, 690)
$graphics = [Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([Drawing.Color]::FromArgb(43, 48, 58))
$graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font = [Drawing.Font]::new('Arial', 11)
$brush = [Drawing.SolidBrush]::new([Drawing.Color]::Ivory)
function Draw-Asset([string]$Name, [int]$X, [int]$Y, [int]$Width, [int]$Height) {
    $bitmap = [Drawing.Bitmap]::new("$artRoot/Assets/UI/Common/$Name.png")
    try { $graphics.DrawImage($bitmap, $X, $Y, $Width, $Height) } finally { $bitmap.Dispose() }
}
function Draw-Sliced([int]$X, [int]$Y, [int]$Width, [int]$Height) {
    $bitmap = [Drawing.Bitmap]::new("$artRoot/Assets/UI/Common/ui_button_green_v01.png")
    $sourceX = @(0,48,464,512); $sourceY = @(0,48,112,160)
    $targetX = @($X,($X+48),($X+$Width-48),($X+$Width))
    $targetY = @($Y,($Y+48),($Y+$Height-48),($Y+$Height))
    try {
        for ($row = 0; $row -lt 3; $row++) { for ($col = 0; $col -lt 3; $col++) {
            $src = [Drawing.Rectangle]::new($sourceX[$col],$sourceY[$row],($sourceX[$col+1]-$sourceX[$col]),($sourceY[$row+1]-$sourceY[$row]))
            $dst = [Drawing.Rectangle]::new($targetX[$col],$targetY[$row],($targetX[$col+1]-$targetX[$col]),($targetY[$row+1]-$targetY[$row]))
            $graphics.DrawImage($bitmap,$dst,$src,[Drawing.GraphicsUnit]::Pixel)
        } }
    } finally { $bitmap.Dispose() }
}
$graphics.DrawString('Batch 22 - existing blue / new green at 480 x 150', $font, $brush, 10, 6)
Draw-Asset 'ui_button_blue_v01' 16 28 480 150
Draw-Asset 'ui_button_green_v01' 16 181 480 150
$graphics.DrawString('Checkboxes: 128 / 64 / 32 px', $font, $brush, 10, 342)
Draw-Asset 'ui_checkbox_off_v01' 12 364 128 128
Draw-Asset 'ui_checkbox_on_v01' 148 364 128 128
Draw-Asset 'ui_checkbox_off_v01' 298 370 64 64
Draw-Asset 'ui_checkbox_on_v01' 375 370 64 64
Draw-Asset 'ui_checkbox_off_v01' 314 452 32 32
Draw-Asset 'ui_checkbox_on_v01' 391 452 32 32
$graphics.DrawString('48 px 9-slice: 240 x 100 / 240 x 160', $font, $brush, 10, 498)
Draw-Sliced 8 520 240 100
Draw-Sliced 264 520 240 160
$canvas.Save("$artRoot/Previews/UI_batch22_contact_v01.png", [Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $canvas.Dispose(); $font.Dispose(); $brush.Dispose()
Write-Output "QC: 3 exact exports; checkbox alpha-mask differences=$($registration[0])/$($registration[1]); contact saved"
