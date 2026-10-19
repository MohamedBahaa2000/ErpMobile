Add-Type -AssemblyName System.Drawing

# صورة مؤقتة لبانر صفحة الدخول (تُستبدل بصورة المستخدم في نفس المسار)
$W = 1400; $H = 900
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# خلفية متدرّجة نهارية (مكتب مضيء)
$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
$bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect,
    [System.Drawing.Color]::FromArgb(255, 226, 236, 248),
    [System.Drawing.Color]::FromArgb(255, 176, 199, 232),
    [float](25))
$g.FillRectangle($bg, $rect) | Out-Null

# دوائر بوكيه ضبابية (إضاءة المكتب)
$rand = New-Object System.Random(7)
$bokehColors = @(
    [System.Drawing.Color]::FromArgb(70, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(45, 210, 226, 248),
    [System.Drawing.Color]::FromArgb(38, 255, 246, 214)
)
for ($i = 0; $i -lt 40; $i++) {
    $d = $rand.Next(60, 260)
    $x = $rand.Next(-80, $W)
    $y = $rand.Next(-60, [int]($H * 0.75))
    $c = $bokehColors[$rand.Next($bokehColors.Count)]
    $b = New-Object System.Drawing.SolidBrush($c)
    $g.FillEllipse($b, $x, $y, $d, $d)
    $b.Dispose()
}

# طاولة اجتماعات (شريط فاتح أسفل) — ناعم بلا عناصر خشنة
$tbl = New-Object System.Drawing.Rectangle(0, [int]($H * 0.66), $W, [int]($H * 0.34))
$tblBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($tbl,
    [System.Drawing.Color]::FromArgb(0, 244, 248, 252),
    [System.Drawing.Color]::FromArgb(120, 226, 234, 245),
    [float](90))
$g.FillRectangle($tblBrush, $tbl)
$tblBrush.Dispose()

# إضاءة ناعمة إضافية (توهج علوي يمين)
$glow = New-Object System.Drawing.Rectangle([int]($W * 0.55), 0, [int]($W * 0.45), [int]($H * 0.5))
$glowBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($glow,
    [System.Drawing.Color]::FromArgb(60, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(0, 255, 255, 255),
    [float](45))
$g.FillEllipse($glowBrush, $glow)
$glowBrush.Dispose()

# تعتيم خفيف في الأعلى لدمج أقوى مع التدرّج الأزرق في التطبيق
$shade = New-Object System.Drawing.Rectangle(0, 0, $W, [int]($H * 0.45))
$shadeBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($shade,
    [System.Drawing.Color]::FromArgb(90, 20, 24, 120),
    [System.Drawing.Color]::FromArgb(0, 20, 24, 120),
    [float](90))
$g.FillRectangle($shadeBrush, $shade)

$g.Dispose()
$bmp.Save("ErpMobile\Resources\Images\login_hero.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved: ErpMobile\Resources\Images\login_hero.png"
