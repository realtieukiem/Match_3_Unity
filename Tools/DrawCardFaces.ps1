# Draws the flat card faces and the bare buff icons the game picks up from Assets/Pokiwar/Art/FG38.
# Windows PowerShell 5.1 + System.Drawing. Re-run to regenerate; -Sheet writes a contact sheet for review.
param(
    [string]$Out = (Join-Path $PSScriptRoot '..\Assets\Pokiwar\Art\FG38'),
    [string]$Sheet = ''
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$W = 432; $H = 552
$Ink = [System.Drawing.Color]::FromArgb(255, 38, 22, 30)

function C([int]$r, [int]$g, [int]$b, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, $r, $g, $b) }
function P([float]$x, [float]$y) { New-Object System.Drawing.PointF $x, $y }
function NewPath { $p = New-Object System.Drawing.Drawing2D.GraphicsPath; $p.FillMode = 'Winding'; $p }
function Poly($pts) { $p = NewPath; $p.AddPolygon([System.Drawing.PointF[]]$pts); $p }
function Round([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = NewPath; $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); $p
}
function Shape($g, $path, $fill, [float]$line = 14) {
    if ($line -gt 0) { $pen = New-Object System.Drawing.Pen $Ink, $line; $pen.LineJoin = 'Round'; $g.DrawPath($pen, $path); $pen.Dispose() }
    $b = New-Object System.Drawing.SolidBrush $fill; $g.FillPath($b, $path); $b.Dispose()
}
function Limb($g, $pts, [float]$w, $color) {
    $pen = New-Object System.Drawing.Pen $color, $w; $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'
    $g.DrawLines($pen, [System.Drawing.PointF[]]$pts); $pen.Dispose()
}
function Canvas([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.Clear([System.Drawing.Color]::Transparent)
    @{ Bmp = $bmp; G = $g }
}
function Save($cv, [string]$name) {
    $cv.G.Dispose(); $path = Join-Path $Out $name
    $cv.Bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $cv.Bmp.Dispose(); $path
}

function Heart { $p = NewPath; $p.AddBezier(0, 88, -150, -10, -70, -112, 0, -45); $p.AddBezier(0, -45, 70, -112, 150, -10, 0, 88); $p.CloseFigure(); $p }
function Bolt { Poly @((P 25 -100), (P -55 10), (P -8 10), (P -28 100), (P 58 -22), (P 10 -22)) }
function Flame {
    $p = NewPath
    $p.AddBezier(0, -105, 30, -50, 88, -20, 72, 40); $p.AddBezier(72, 40, 60, 100, -60, 100, -72, 40)
    $p.AddBezier(-72, 40, -82, 0, -42, -20, -36, -58); $p.AddBezier(-36, -58, -20, -30, -6, -60, 0, -105); $p.CloseFigure(); $p
}
function ShieldPath {
    $p = NewPath; $p.AddLine(-82, -76, 0, -102); $p.AddLine(0, -102, 82, -76)
    $p.AddBezier(82, -76, 86, 30, 50, 76, 0, 102); $p.AddBezier(0, 102, -50, 76, -86, 30, -82, -76); $p.CloseFigure(); $p
}
function Glyph($g, [float]$cx, [float]$cy, [float]$s, [scriptblock]$draw) {
    $state = $g.Save(); $g.TranslateTransform($cx, $cy); $g.ScaleTransform($s, $s); & $draw; $g.Restore($state)
}

function Background($g, $top, $bottom, $wedge) {
    $card = Round 0 0 $W $H 40
    $g.SetClip($card)
    $rect = New-Object System.Drawing.RectangleF 0, 0, $W, $H
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $top, $bottom, 90
    $g.FillRectangle($grad, $rect); $grad.Dispose()
    $wb = New-Object System.Drawing.SolidBrush $wedge
    $g.FillPolygon($wb, [System.Drawing.PointF[]]@((P 0 0), (P ($W * 0.86) 0), (P 0 ($H * 0.66)))); $wb.Dispose()
    $g.ResetClip()
    $edge = New-Object System.Drawing.Pen (C 255 255 255 150), 10
    $g.DrawPath($edge, (Round 9 9 ($W - 18) ($H - 18) 32)); $edge.Dispose()
}

$Looks = @{
    mana   = @((C 110 200 255), (C 26 92 208), (C 255 255 255 80))
    hp     = @((C 156 236 96), (C 34 146 58), (C 255 252 150 95))
    shield = @((C 190 146 255), (C 92 48 192), (C 255 255 255 75))
    attack = @((C 96 104 140), (C 38 40 64), (C 255 204 44 215))
    leech  = @((C 124 218 226), (C 26 116 136), (C 255 255 255 75))
    skill  = @((C 255 196 54), (C 238 100 22), (C 255 240 130 150))
}
function Face([string]$name, [string]$look, [scriptblock]$draw) {
    $cv = Canvas $W $H; $l = $Looks[$look]
    Background $cv.G $l[0] $l[1] $l[2]
    & $draw $cv.G
    Save $cv ("face_" + $name + ".png")
}

$made = @()
$gx = 272; $gy = 190

$made += Face 'mana_potion' 'mana' { param($g) Glyph $g $gx $gy 1.0 {
    $flask = NewPath; $flask.AddEllipse(-72, -38, 144, 144); $flask.AddRectangle((New-Object System.Drawing.RectangleF -24, -98, 48, 80))
    Shape $g $flask (C 255 255 255)
    $st = $g.Save(); $clip = NewPath; $clip.AddEllipse(-60, -26, 120, 120); $g.SetClip($clip, 'Intersect')
    $b = New-Object System.Drawing.SolidBrush (C 60 150 255); $g.FillRectangle($b, -80, 14, 160, 100); $b.Dispose(); $g.Restore($st)
    Shape $g (Round -38 -112 76 26 8) (C 255 255 255)
    Glyph $g 0 46 0.42 { Shape $g (Bolt) (C 255 226 70) 16 }
} }

$made += Face 'herbal_salve' 'hp' { param($g) Glyph $g $gx $gy 1.0 {
    Shape $g (Heart) (C 255 255 255)
    $b = New-Object System.Drawing.SolidBrush (C 40 170 70)
    $g.FillRectangle($b, -13, -42, 26, 84); $g.FillRectangle($b, -42, -13, 84, 26); $b.Dispose()
} }

$made += Face 'iron_skin' 'shield' { param($g) Glyph $g $gx $gy 1.0 {
    Shape $g (ShieldPath) (C 255 255 255)
    Glyph $g 0 0 0.66 { Shape $g (ShieldPath) (C 140 90 235) 0 }
} }

$made += Face 'fire_bolt' 'attack' { param($g) Glyph $g $gx $gy 1.0 {
    $ball = NewPath; $ball.AddPolygon([System.Drawing.PointF[]]@((P -100 -100), (P 66 -14), (P -6 66))); $ball.AddEllipse(-30, -24, 120, 120)
    Shape $g $ball (C 255 130 36)
    $core = NewPath; $core.AddPolygon([System.Drawing.PointF[]]@((P -52 -52), (P 44 6), (P 2 46))); $core.AddEllipse(-2, 4, 64, 64)
    Shape $g $core (C 255 232 90) 0
} }

$made += Face 'meteor' 'attack' { param($g) Glyph $g $gx $gy 1.0 {
    Shape $g (Poly @((P -30 -10), (P 104 -108), (P 56 36))) (C 255 130 36)
    Shape $g (Poly @((P -8 -6), (P 70 -66), (P 40 22))) (C 255 232 90) 0
    Shape $g (Poly @((P -84 22), (P -56 -30), (P -4 -40), (P 40 -8), (P 46 46), (P 6 90), (P -54 80))) (C 150 112 96)
    $b = New-Object System.Drawing.SolidBrush (C 104 74 66); $g.FillEllipse($b, -50, 10, 30, 24); $g.FillEllipse($b, -8, 36, 24, 20); $b.Dispose()
} }

$made += Face 'summon_sprite' 'attack' { param($g) Glyph $g $gx $gy 1.0 {
    foreach ($side in -1, 1) {
        $st = $g.Save(); $g.TranslateTransform(58 * $side, -22); $g.RotateTransform(28 * $side)
        $wing = NewPath; $wing.AddEllipse(-34, -62, 68, 124); Shape $g $wing (C 196 244 255); $g.Restore($st)
    }
    Shape $g (Poly @((P 0 -30), (P -42 86), (P 42 86))) (C 255 255 255)
    $head = NewPath; $head.AddEllipse(-32, -84, 64, 64); Shape $g $head (C 255 255 255)
} }

$made += Face 'war_cry' 'attack' { param($g) Glyph $g $gx $gy 1.0 {
    Glyph $g -30 0 1.0 {
        Shape $g (Poly @((P 0 -108), (P 21 -80), (P 21 36), (P -21 36), (P -21 -80))) (C 255 255 255)
        Shape $g (Round -54 34 108 22 6) (C 255 204 60)
        Shape $g (Round -12 56 24 40 4) (C 150 100 70)
        $pm = NewPath; $pm.AddEllipse(-14, 90, 28, 28); Shape $g $pm (C 255 204 60)
    }
    Shape $g (Poly @((P 72 -84), (P 112 -34), (P 88 -34), (P 88 26), (P 56 26), (P 56 -34), (P 32 -34))) (C 120 230 84)
} }

$made += Face 'mana_leech' 'leech' { param($g) Glyph $g $gx $gy 1.0 {
    Glyph $g 30 -18 0.82 { Shape $g (Bolt) (C 70 160 255) 16 }
    Shape $g (Poly @((P -104 100), (P -104 30), (P -84 50), (P -34 0), (P -4 30), (P -54 80), (P -34 100))) (C 255 255 255)
} }

$cv = Canvas $W $H; $l = $Looks['skill']; $g = $cv.G
Background $g $l[0] $l[1] $l[2]
$star = @(); for ($i = 0; $i -lt 24; $i++) { $r = if ($i % 2 -eq 0) { 132 } else { 66 }; $a = [Math]::PI * $i / 12; $star += P (300 + $r * [Math]::Cos($a)) (268 + $r * [Math]::Sin($a)) }
Shape $g (Poly $star) (C 255 240 96) 0
$core = @(); for ($i = 0; $i -lt 24; $i++) { $r = if ($i % 2 -eq 0) { 74 } else { 38 }; $a = [Math]::PI * $i / 12; $core += P (300 + $r * [Math]::Cos($a)) (268 + $r * [Math]::Sin($a)) }
Shape $g (Poly $core) (C 255 255 255) 0
$body = C 30 18 24
Limb $g @((P 168 252), (P 104 300), (P 128 344)) 30 $body
Limb $g @((P 196 356), (P 126 412), (P 84 478)) 38 $body
Limb $g @((P 196 356), (P 262 404), (P 250 484)) 38 $body
Limb $g @((P 168 232), (P 196 352)) 54 $body
Limb $g @((P 178 248), (P 286 262)) 32 $body
$hb = New-Object System.Drawing.SolidBrush $body
$g.FillEllipse($hb, 110, 138, 76, 76); $g.FillEllipse($hb, 276, 236, 46, 54); $hb.Dispose()
$made += Save $cv 'face_skill.png'

function Buff([string]$name, [scriptblock]$draw) {
    $cv = Canvas 256 256
    Glyph $cv.G 128 128 0.98 $draw.GetNewClosure()
    Save $cv ("buff_" + $name + ".png")
}
$bg = $null
$c = Canvas 256 256; $g = $c.G
Glyph $g 128 132 0.98 { Shape $g (Heart) (C 96 226 104) 18; $b = New-Object System.Drawing.SolidBrush (C 255 255 255); $g.FillRectangle($b, -12, -40, 24, 78); $g.FillRectangle($b, -39, -13, 78, 24); $b.Dispose() }
$made += Save $c 'buff_heart.png'
$c = Canvas 256 256; $g = $c.G
Glyph $g 128 128 1.05 { Shape $g (Bolt) (C 84 176 255) 18; Glyph $g 2 -4 0.5 { Shape $g (Bolt) (C 220 242 255) 0 } }
$made += Save $c 'buff_lightning.png'
$c = Canvas 256 256; $g = $c.G
Glyph $g 128 130 1.05 { Shape $g (Flame) (C 255 120 40) 18; Glyph $g 0 38 0.5 { Shape $g (Flame) (C 255 226 90) 0 } }
$made += Save $c 'buff_fire.png'
$c = Canvas 256 256; $g = $c.G
Glyph $g 128 128 1.05 { Shape $g (ShieldPath) (C 170 120 250) 18; Glyph $g 0 0 0.6 { Shape $g (ShieldPath) (C 226 204 255) 0 } }
$made += Save $c 'buff_shield.png'

if ($Sheet) {
    $cols = 9; $tw = 216; $th = 276
    $board = New-Object System.Drawing.Bitmap ($cols * $tw), ($th + 150)
    $sg = [System.Drawing.Graphics]::FromImage($board); $sg.Clear((C 60 50 46)); $sg.InterpolationMode = 'HighQualityBicubic'
    $i = 0; $j = 0
    foreach ($f in $made) {
        $img = [System.Drawing.Image]::FromFile($f)
        if ($img.Width -eq $W) { $sg.DrawImage($img, $i * $tw + 4, 4, $tw - 8, $th - 8); $i++ }
        else { $sg.DrawImage($img, $j * 140 + 10, $th + 10, 128, 128); $j++ }
        $img.Dispose()
    }
    $sg.Dispose(); $board.Save($Sheet, [System.Drawing.Imaging.ImageFormat]::Png); $board.Dispose()
}
$made | ForEach-Object { Split-Path $_ -Leaf }
