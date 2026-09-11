# make-waves.ps1 — bake the turn-7A header wave shape as per-accent,
# PER-LAYER PNGs at EXACT rendered size (seam escalation 2026-07-05):
# every proven seamless tiler in the codebase (BbIconScroll patterns,
# gauge-stripe) tiles at NATIVE size — background-size scaling at
# sample time was the remaining seam suspect, so the scale moved into
# the BAKE. Layer A renders 1600x70, layer B 1600x53 (the strip divs
# crop the bottom 12/18px — the mock's overflow crop).
#
# Path (600x90 design space, seamless horizontal tile — starts/ends at
# y45 with matching tangents):
#   M0,45 C50,25 100,25 150,45 C200,65 250,65 300,45
#         C350,25 400,25 450,45 C500,65 550,65 600,45 L600,90 L0,90 Z
# SEAM FIX #1 (kept): the path is drawn extended ONE SEGMENT past both
# canvas edges so the edge pixel columns are INTERIOR curve samples —
# boundary AA otherwise leaves semi-transparent edge pixels that
# bilinear repeat-sampling draws as a hairline at every tile join.
# One PNG per accent per layer — s&box cannot CSS-tint (filter law);
# layer OPACITY stays CSS.

Add-Type -AssemblyName System.Drawing

# Desktop repo path (2026-08-28 — the old laptop OneDrive path is gone)
$outDir = "c:\users\jscho\documents\s&box projects\megarougelite\Assets\ui"
$accents = [ordered]@{
    'violet' = '#8B5CF6'   # collection (My Beasts)
    'green'  = '#4ADE80'   # expedition
    'red'    = '#F87171'   # battle
    'pink'   = '#F472B6'   # fusion (RETIRED 2026-07-12 — fusion now rides violet)
    'gold'   = '#FBBF24'   # journal
    'appink' = '#FF6BD6'   # skills (Tamer Talents — the PawPad SKILLS app pink)
    'teal'   = '#2DD4BF'   # beastbook (Specimen Hall — the PawPad BEASTBOOK app teal, 2026-07-12)
    'blue'   = '#3F8FE0'   # online (TamerLink — the PawPad ONLINE app blue, 2026-08-28)
    'quests' = '#3fb45e'   # quests (the PawPad QUESTS app green, 2026-09-11) — NOT 'green' (#4ADE80 = expedition)
    # PawPad in-phone APP accents (2026-08-28) - phone-scale header wave strips for the
    # four apps; the 512px screen crops the 1600 tile at native size (no scaling).
    'chat'    = '#4AA8FF'
    'radio'   = '#C26BFF'
    'effects' = '#F7E024'
    'alerts'  = '#E0414A'
}
# layer key -> rendered tile height (width is always the 1600px period)
$layers = [ordered]@{ 'a' = 70; 'b' = 53 }
$W = 1600

foreach ($name in $accents.Keys) {
    $col = [System.Drawing.ColorTranslator]::FromHtml($accents[$name])
    foreach ($layer in $layers.Keys) {
        $H = [int]$layers[$layer]
        $sx = $W / 600.0; $sy = $H / 90.0

        $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)

        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddBezier(-150.0, 45.0, -100.0, 65.0, -50.0, 65.0,  0.0, 45.0)   # wrapped trough (last segment shifted -600)
        $path.AddBezier(0.0, 45.0,   50.0, 25.0,  100.0, 25.0,  150.0, 45.0)
        $path.AddBezier(150.0, 45.0, 200.0, 65.0, 250.0, 65.0,  300.0, 45.0)
        $path.AddBezier(300.0, 45.0, 350.0, 25.0, 400.0, 25.0,  450.0, 45.0)
        $path.AddBezier(450.0, 45.0, 500.0, 65.0, 550.0, 65.0,  600.0, 45.0)
        $path.AddBezier(600.0, 45.0, 650.0, 25.0, 700.0, 25.0,  750.0, 45.0)   # wrapped crest (first segment shifted +600)
        $path.AddLine(750.0, 45.0, 750.0, 90.0)
        $path.AddLine(750.0, 90.0, -150.0, 90.0)
        $path.CloseFigure()

        $m = New-Object System.Drawing.Drawing2D.Matrix([float]$sx, 0.0, 0.0, [float]$sy, 0.0, 0.0)
        $path.Transform($m)

        $brush = New-Object System.Drawing.SolidBrush($col)
        $g.FillPath($brush, $path)

        $out = Join-Path $outDir "wave-$name-$layer.png"
        $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "wrote $out"

        $brush.Dispose(); $path.Dispose(); $g.Dispose(); $bmp.Dispose()
    }
}


# ---------------------------------------------------------------------------
# PHONE-PERIOD WAVES (2026-08-28): the 1600px page tile cropped to the 512px
# PawPad screen shows one random curve, not a wave. These tiles use a 512px
# period (one full crest+trough per screen width) at phone heights 36 / 28.
# Files: wave-<app>-phone-a.png (512x36), wave-<app>-phone-b.png (512x28).
# ---------------------------------------------------------------------------
$phoneAccents = [ordered]@{
    'chat'    = '#4AA8FF'
    'radio'   = '#C26BFF'
    'effects' = '#F7E024'
    'alerts'  = '#E0414A'
}
$phoneLayers = [ordered]@{ 'a' = 36; 'b' = 28 }
$PW = 512
foreach ($name in $phoneAccents.Keys) {
    $col = [System.Drawing.ColorTranslator]::FromHtml($phoneAccents[$name])
    foreach ($layer in $phoneLayers.Keys) {
        $H = [int]$phoneLayers[$layer]
        $sx = $PW / 600.0; $sy = $H / 90.0
        $bmp = New-Object System.Drawing.Bitmap($PW, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddBezier(-150.0, 45.0, -100.0, 65.0, -50.0, 65.0,  0.0, 45.0)
        $path.AddBezier(0.0, 45.0,   50.0, 25.0,  100.0, 25.0,  150.0, 45.0)
        $path.AddBezier(150.0, 45.0, 200.0, 65.0, 250.0, 65.0,  300.0, 45.0)
        $path.AddBezier(300.0, 45.0, 350.0, 25.0, 400.0, 25.0,  450.0, 45.0)
        $path.AddBezier(450.0, 45.0, 500.0, 65.0, 550.0, 65.0,  600.0, 45.0)
        $path.AddBezier(600.0, 45.0, 650.0, 25.0, 700.0, 25.0,  750.0, 45.0)
        $path.AddLine(750.0, 45.0, 750.0, 90.0)
        $path.AddLine(750.0, 90.0, -150.0, 90.0)
        $path.CloseFigure()
        $m = New-Object System.Drawing.Drawing2D.Matrix([float]$sx, 0.0, 0.0, [float]$sy, 0.0, 0.0)
        $path.Transform($m)
        $brush = New-Object System.Drawing.SolidBrush($col)
        $g.FillPath($brush, $path)
        $out = Join-Path $outDir "wave-$name-phone-$layer.png"
        $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "wrote $out"
        $brush.Dispose(); $path.Dispose(); $g.Dispose(); $bmp.Dispose()
    }
}
