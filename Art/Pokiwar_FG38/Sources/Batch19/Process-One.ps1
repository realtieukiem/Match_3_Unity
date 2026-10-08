param(
    [Parameter(Mandatory)][string]$Id,
    [Parameter(Mandatory)][string]$RawSource,
    [string]$Revision = 'r01',
    [string]$KernelPath = (Join-Path $env:USERPROFILE '.codex/skills/art-2d/scripts/sprite-kernels.cs')
)
$ErrorActionPreference = 'Stop'
$artRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$definitions = Get-Content -Raw -Encoding UTF8 -LiteralPath "$PSScriptRoot/definitions.json" | ConvertFrom-Json
$definition = $definitions | Where-Object { $_.key -eq $Id }
if (-not $definition) { throw "Unknown asset: $Id" }
$target = Join-Path "$artRoot/Assets" $definition.file
if (Test-Path -LiteralPath $target) { throw "Output already exists: $target" }
$rawCopy = Join-Path $PSScriptRoot "$Id.$Revision.raw.png"
if (Test-Path -LiteralPath $rawCopy) { throw "Raw revision already exists: $rawCopy" }
Copy-Item -LiteralPath $RawSource -Destination $rawCopy
Add-Type -Path $KernelPath -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
using System;
public static class Batch19Pixels {
    public static int Normalize(byte[] p) {
        int removed = 0;
        for (int i = 0; i < p.Length; i += 4) {
            if (p[i + 3] == 1) { Array.Clear(p, i, 4); removed++; }
        }
        return removed;
    }
    public static int[] Check(byte[] p, int w, int h, bool card) {
        int edges = 0, holes = 0, minAlpha = 255;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int a = p[(y * w + x) * 4 + 3];
            minAlpha = Math.Min(minAlpha, a);
            if ((x == 0 || y == 0 || x == w - 1 || y == h - 1) && a > 0) edges++;
            if (card && x > w * .12 && x < w * .88 && y > h * .12 && y < h * .88 && a < 254) holes++;
        }
        return new[] { edges, holes, minAlpha };
    }
}
'@
$raw = [Art2D.Img]::Load($rawCopy)
$isCard = $definition.file.StartsWith('Cards/')
$keyReport = if ($definition.matte) { [Art2D.Kit]::Key($raw, 60, 2, 40, $false, 255, 0, 255) | ConvertFrom-Json } else { $null }
$removed = if ($definition.transparent) { [Batch19Pixels]::Normalize($raw.P) } else { 0 }
$raw.Save("$PSScriptRoot/$Id.$Revision.processed.png")
$inspection = [Art2D.Kit]::Inspect($raw, $Id) | ConvertFrom-Json
if ($definition.transparent) {
    $bounds = $inspection.content_box
    if ($inspection.touches_edge -or $bounds[2] -le 0 -or $bounds[3] -le 0) { throw "Invalid source bounds: $Id" }
    $crop = $raw.Crop($bounds[0], $bounds[1], $bounds[2], $bounds[3])
    if ($isCard -and [Math]::Abs(($crop.W / [double]$crop.H) / ($definition.width / [double]$definition.height) - 1.0) -gt .06) {
        throw "Card aspect ratio differs from its production frame: $Id"
    }
    $marginX = if ($isCard) { 2 } else { [int][Math]::Ceiling($definition.width * .06) }
    $marginY = if ($isCard) { 2 } else { [int][Math]::Ceiling($definition.height * .06) }
    $scale = [Math]::Min(($definition.width - 2 * $marginX) / [double]$crop.W, ($definition.height - 2 * $marginY) / [double]$crop.H)
    $width = [int][Math]::Floor($crop.W * $scale)
    $height = [int][Math]::Floor($crop.H * $scale)
    $output = [Art2D.Img]::new($definition.width, $definition.height)
    $scaled = [Art2D.Kit]::Resample($crop, $width, $height, $false)
    $left = [int][Math]::Floor(($definition.width - $width) / 2.0)
    $top = [int][Math]::Floor(($definition.height - $height) / 2.0)
    if (-not $output.Blit($scaled, $left, $top)) { throw "Export clipped: $Id" }
} else {
    $output = [Art2D.Kit]::Resample($raw, $definition.width, $definition.height, $false)
}
$stats = [Batch19Pixels]::Check($output.P, $output.W, $output.H, $isCard)
if ($definition.transparent -and ($stats[0] -ne 0 -or $stats[1] -ne 0)) { throw "Invalid alpha: $Id, border=$($stats[0]), cardHoles=$($stats[1])" }
if (-not $definition.transparent -and $stats[2] -ne 255) { throw "Background is not fully opaque: $Id" }
New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
$output.Save($target)
$previewWidth = [int][Math]::Min(256, $output.W)
$previewHeight = [int][Math]::Max(1, [Math]::Round($output.H * $previewWidth / [double]$output.W))
$preview = [Art2D.Kit]::Resample($output, $previewWidth, $previewHeight, $false)
$preview.Save("$PSScriptRoot/$Id.$Revision.preview.png")
$report = [ordered]@{
    id = $Id; file = $definition.file; revision = $Revision
    rawSource = $RawSource; rawCopy = "Sources/Batch19/$Id.$Revision.raw.png"
    rawHash = (Get-FileHash -LiteralPath $rawCopy).Hash
    promptFile = "Sources/Batch19/$Id.$Revision.prompt.txt"
    sourceInspection = $inspection; key = $keyReport; removedAlphaOne = $removed
    outputInspection = ([Art2D.Kit]::Inspect($output, $Id) | ConvertFrom-Json)
    edgePixels = $stats[0]; interiorAlphaHoles = $stats[1]; minimumAlpha = $stats[2]
    verification = 'V1'; origin = 'generated'; tool = 'built-in image_gen'
}
[IO.File]::WriteAllText("$PSScriptRoot/$Id.$Revision.export.json", ($report | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
$definitions = Get-Content -Raw -Encoding UTF8 -LiteralPath "$PSScriptRoot/definitions.json" | ConvertFrom-Json
$statePath = Join-Path $PSScriptRoot 'RUN_STATE.json'
$state = Get-Content -Raw -Encoding UTF8 -LiteralPath $statePath | ConvertFrom-Json
$state.completed = @($definitions | Where-Object { Test-Path -LiteralPath (Join-Path "$artRoot/Assets" $_.file) } | ForEach-Object file)
$state.remaining = @($definitions | Where-Object { $_.file -notin $state.completed } | ForEach-Object file)
[IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
"$Id exported $($output.W)x$($output.H); border=$($stats[0]); interiorHoles=$($stats[1]); completed=$($state.completed.Count)/5"
