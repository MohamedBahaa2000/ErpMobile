Add-Type -AssemblyName System.Drawing

# ===================== أيقونة التطبيق (مربّعة: Select فوق ERP) =====================
function New-ErpIcon {
    param([string]$Path, [int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    # خلفية متدرجة زي لوجو Select ERP
    $rect = New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect,
        [System.Drawing.Color]::FromArgb(255, 40, 33, 232),
        [System.Drawing.Color]::FromArgb(255, 55, 183, 234),
        [float](45))
    $g.FillRectangle($brush, $rect) | Out-Null

    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Near

    # كلمة Select (بيضاء عريضة)
    $selFont = New-Object System.Drawing.Font("Arial", [float]($Size * 0.215), [System.Drawing.FontStyle]::Bold)
    $white = [System.Drawing.Brushes]::White
    $selRect = New-Object System.Drawing.RectangleF(0, [float]($Size * 0.16), [float]$Size, [float]($Size * 0.25))
    $g.DrawString("Select", $selFont, $white, $selRect, $sf)

    # شارة ERP — نقيس النص ونحدّد عرض الشارة حسبه (مش بتتقص)
    $erpFont = New-Object System.Drawing.Font("Arial", [float]($Size * 0.13), [System.Drawing.FontStyle]::Bold)
    $badgeSize = $g.MeasureString("ERP", $erpFont)
    $badgeW = [int]($badgeSize.Width + ($Size * 0.10))
    $badgeH = [int]($Size * 0.26)
    $badgeX = [int](($Size - $badgeW) / 2)
    $badgeY = [int]($Size * 0.50)
    $badgeRect = New-Object System.Drawing.Rectangle($badgeX, $badgeY, $badgeW, $badgeH)
    $badgeBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($badgeRect,
        [System.Drawing.Color]::FromArgb(255, 23, 59, 112),
        [System.Drawing.Color]::FromArgb(255, 36, 88, 242),
        [float](0))
    $radius = [int]($Size * 0.055)

    # رسم مسار مدوّر للشارة
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x = $badgeRect.X; $y = $badgeRect.Y; $w = $badgeRect.Width; $h = $badgeRect.Height
    $d = $radius * 2
    $gp.AddArc($x, $y, $d, $d, 180, 90)
    $gp.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $gp.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $gp.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $gp.CloseFigure()
    $g.FillPath($badgeBrush, $gp)

    $erpRect = New-Object System.Drawing.RectangleF([float]$x, [float]($y + $h * 0.18), [float]$w, [float]($h * 0.7))
    $g.DrawString("ERP", $erpFont, $white, $erpRect, $sf)

    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "saved: $Path"
}

# ===================== سبلاش صفحة البداية (نفس شكل اللوجو: Select + ERP جنب بعض) =====================
function New-ErpSplash {
    param([string]$Path, [int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    # نفس خلفية الـ appicon (أزرق متدرج)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect,
        [System.Drawing.Color]::FromArgb(255, 40, 33, 232),
        [System.Drawing.Color]::FromArgb(255, 55, 183, 234),
        [float](45))
    $g.FillRectangle($brush, $rect) | Out-Null

    $white = [System.Drawing.Brushes]::White

    # نصوص اللوجو: Select + ERP — نقيس ونقلّ الحجم تلقائياً حتى يناسب المساحة
    $maxW = [int]($Size * 0.86)   # أقصى عرض مسموح للوجو
    $selScale = 0.17
    $erpScale = 0.10

    $selFont = New-Object System.Drawing.Font("Arial", [float]($Size * $selScale), [System.Drawing.FontStyle]::Bold)
    $erpFont = New-Object System.Drawing.Font("Arial", [float]($Size * $erpScale), [System.Drawing.FontStyle]::Bold)

    $gap       = [int]($Size * 0.028)
    $badgePadX = [int]($Size * 0.038)
    $badgePadY = [int]($Size * 0.022)

    # قياس + تصغير تلقائي حتى يتّسع اللوجو كامل داخل الإطار
    for ($i = 0; $i -lt 12; $i++) {
        $selSize = $g.MeasureString("Select", $selFont)
        $erpSize = $g.MeasureString("ERP", $erpFont)
        $badgeW  = [int]($erpSize.Width + 2 * $badgePadX)
        $totalW  = [int]($selSize.Width + $gap + $badgeW)
        if ($totalW -le $maxW) { break }
        $selScale = $selScale * 0.94
        $erpScale = $erpScale * 0.94
        $selFont.Dispose(); $erpFont.Dispose()
        $selFont = New-Object System.Drawing.Font("Arial", [float]($Size * $selScale), [System.Drawing.FontStyle]::Bold)
        $erpFont = New-Object System.Drawing.Font("Arial", [float]($Size * $erpScale), [System.Drawing.FontStyle]::Bold)
    }

    $badgeH   = [int]($selSize.Height * 0.86)
    $startX   = [int](($Size - $totalW) / 2)
    $midY     = [int]($Size / 2)

    # رسم Select (أبيض عريض) — بالمنتصف رأسياً
    $selRect = New-Object System.Drawing.RectangleF(
        [float]$startX,
        [float]($midY - $selSize.Height / 2),
        [float]$selSize.Width,
        [float]$selSize.Height)
    $sfSel = New-Object System.Drawing.StringFormat
    $sfSel.Alignment = [System.Drawing.StringAlignment]::Near
    $sfSel.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString("Select", $selFont, $white, $selRect, $sfSel)

    # شارة ERP (مدوّرة بتدرّج أزرق) — جنب Select على نفس السطر
    $badgeX = [int]($startX + $selSize.Width + $gap)
    $badgeY = [int]($midY - $badgeH / 2)
    $radius = [int]($Size * 0.022)

    $badgeRect = New-Object System.Drawing.Rectangle($badgeX, $badgeY, $badgeW, $badgeH)
    $badgeBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($badgeRect,
        [System.Drawing.Color]::FromArgb(255, 23, 59, 112),
        [System.Drawing.Color]::FromArgb(255, 36, 88, 242),
        [float](0))

    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x = $badgeRect.X; $y = $badgeRect.Y; $w = $badgeRect.Width; $h = $badgeRect.Height
    $d = $radius * 2
    $gp.AddArc($x, $y, $d, $d, 180, 90)
    $gp.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $gp.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $gp.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $gp.CloseFigure()
    $g.FillPath($badgeBrush, $gp)

    $erpRect = New-Object System.Drawing.RectangleF([float]($x + $badgePadX), [float]($y + $badgePadY), [float]($w - 2 * $badgePadX), [float]($h - 2 * $badgePadY))
    $sfErp = New-Object System.Drawing.StringFormat
    $sfErp.Alignment = [System.Drawing.StringAlignment]::Center
    $sfErp.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString("ERP", $erpFont, $white, $erpRect, $sfErp)

    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "saved: $Path"
}

New-ErpIcon   -Path "ErpMobile\Resources\AppIcon\appicon.png" -Size 1024
New-ErpSplash -Path "ErpMobile\Resources\Splash\splash.png"   -Size 1024
