# make-dais.ps1 — bake the element dais (turn 5A/6A) as per-element PNGs.
# CSS could not draw it (live-verified 2026-07-05): border-radius:50% on a
# wide-short div caps at a stadium pill (no ellipse), and the percent-stop
# radial painted near-solid. So the whole platter + inner ring is baked:
#   canvas 600x148 = exact 2x of the 300x74 display size
#   outer ellipse: PathGradientBrush wash, center a=110 (0.43; raised from
#     the mock's 0.28 on 2026-07-05 live capture - a whisper on our dark
#     stage) -> 0.10 at
#     60% -> 0 at 78% (ColorBlend positions run boundary 0 -> center 1)
#     + 2px (=1px display) rim at a=56 (0.22)
#   inner ring: 420x96 (210x48 display), bottom 20px (10 display) above
#     the platter bottom, 2px rim at a=36 (0.14)
# One PNG per element — s&box cannot CSS-tint (filter law).

Add-Type -AssemblyName System.Drawing

$outDir = "c:\Users\jscho\OneDrive\Documents\s&box projects\beastborne\Assets\ui\dais"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

# Turn-6 palette (matches the bestiary icons; keys = ElementType lowercase).
$elements = [ordered]@{
    'neutral'  = @(168, 162, 158)
    'fire'     = @(251, 146, 60)
    'water'    = @(96, 165, 250)
    'nature'   = @(74, 222, 128)
    'wind'     = @(45, 212, 191)
    'electric' = @(251, 191, 36)
    'earth'    = @(201, 151, 76)
    'ice'      = @(125, 211, 252)
    'metal'    = @(148, 163, 184)
    'spirit'   = @(244, 114, 182)
    'shadow'   = @(157, 123, 234)
}

$W = 600; $H = 148

foreach ($name in $elements.Keys) {
    $c = $elements[$name]
    $r = [int]$c[0]; $g_ = [int]$c[1]; $b = [int]$c[2]

    $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # ── outer platter wash (PathGradientBrush over the full ellipse)
    $pRect = New-Object System.Drawing.RectangleF(1, 1, ($W - 2), ($H - 2))
    $ellipse = New-Object System.Drawing.Drawing2D.GraphicsPath
    $ellipse.AddEllipse($pRect)
    $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($ellipse)
    $pgb.CenterPoint = New-Object System.Drawing.PointF(($W / 2.0), ($H / 2.0))
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend(4)
    # positions: 0 = ellipse boundary, 1 = center. Mock stops (center 0.28,
    # 0.06 @ 60%, 0 @ 78%) mirror to boundary-space 0 / 0.22 / 0.40 / 1.
    $blend.Colors = @(
        [System.Drawing.Color]::FromArgb(0,  $r, $g_, $b),
        [System.Drawing.Color]::FromArgb(0,  $r, $g_, $b),
        [System.Drawing.Color]::FromArgb(26, $r, $g_, $b),
        [System.Drawing.Color]::FromArgb(110, $r, $g_, $b)
    )
    $blend.Positions = [float[]]@(0.0, 0.22, 0.40, 1.0)
    $pgb.InterpolationColors = $blend
    $g.FillPath($pgb, $ellipse)

    # ── platter rim (1px display = 2px here, alpha 0.22)
    $penRim = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(86, $r, $g_, $b), 2.0)
    $g.DrawEllipse($penRim, 1, 1, ($W - 2), ($H - 2))

    # ── inner ring (210x48 display -> 420x96, bottom 10 display above the
    #    platter bottom -> y = 148 - 20 - 96 = 32), alpha 0.14
    $penRing = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(56, $r, $g_, $b), 2.0)
    $g.DrawEllipse($penRing, (($W - 420) / 2), 32, 420, 96)

    $out = Join-Path $outDir "dais-$name.png"
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "wrote $out"

    $penRing.Dispose(); $penRim.Dispose(); $pgb.Dispose(); $ellipse.Dispose(); $g.Dispose(); $bmp.Dispose()
}
