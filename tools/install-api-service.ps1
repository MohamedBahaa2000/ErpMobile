<#
═══════════════════════════════════════════════════════════════════
  تشغيل الـ API بشكل دائم (ما يوقفش تاني)
  ─────────────────────────────────────────────────────────────────
  شغّل هذا الملف مرة واحدة على جهاز 192.168.100.70
  بصلاحيات Administrator (كل يمين PowerShell كمسؤول).

  بعدها:
    • الـ API بيبدأ لوحده مع تشغيل الجهاز
    • لو وقع لأي سبب → بيرجع لوحده خلال دقيقة
    • تقدر تقفل الكونسول وتطوّر VS عادي — مش هيأثر
═══════════════════════════════════════════════════════════════════
#>

param(
    # ⚠️ غيّر المسار ده لمسار مشروع الـ API بتاعك على .70
    [string]$Project  = "D:\ErpApi\ErpMobile.Api.csproj",

    [string]$InstallDir = "C:\ErpApi",
    [string]$TaskName   = "ErpApi"
)

$ErrorActionPreference = "Stop"

Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   تثبيت الـ API كخدمة دائمة" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

# ── 1) التحقق من المسار ──────────────────────────────────────────
if (-not (Test-Path $Project)) {
    Write-Host "❌ ما لقيت الملف: $Project" -ForegroundColor Red
    Write-Host "   افتح الملف ده وعدّل قيمة `$Project لمسار مشروعك." -ForegroundColor Yellow

    Write-Host "`n   مشاريع ممكن تكون هي:" -ForegroundColor Gray
    Get-ChildItem -Path C:\, D:\ -Filter *.csproj -Recurse -Depth 4 -ErrorAction SilentlyContinue |
        Select-Object -First 10 -ExpandProperty FullName |
        ForEach-Object { Write-Host "     $_" -ForegroundColor Gray }

    exit 1
}

Write-Host "[1/4] نشر الـ API (Release)..." -ForegroundColor White
dotnet publish $Project -c Release -o $InstallDir
if ($LASTEXITCODE -ne 0) { Write-Host "❌ فشل النشر" -ForegroundColor Red; exit 1 }

# ── 2) إيجاد ملف التشغيل ────────────────────────────────────────
$exe = Get-ChildItem $InstallDir -Filter *.exe -File |
       Where-Object { $_.Name -notlike "*test*" } |
       Select-Object -First 1

if (-not $exe) {
    Write-Host "❌ ما لقيت ملف .exe بعد النشر" -ForegroundColor Red
    exit 1
}
Write-Host "   ✅ ملف التشغيل: $($exe.FullName)" -ForegroundColor Green

# ── 3) تسجيل مهمة تبدأ مع الويندوز + إعادة تشغيل تلقائية ────────
Write-Host "[2/4] إنشاء مهمة بدء التشغيل..." -ForegroundColor White

Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue

$action  = New-ScheduledTaskAction -Execute $exe.FullName -WorkingDirectory $InstallDir
$trigger = New-ScheduledTaskTrigger -AtStartup

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Days 3650) `
    -RestartCount 999 `
    -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -User "SYSTEM" `
    -RunLevel Highest `
    -Force | Out-Null

Write-Host "   ✅ المهمة اتسجلت: $TaskName" -ForegroundColor Green

# ── 4) تشغيلها فوراً ─────────────────────────────────────────────
Write-Host "[3/4] تشغيل الـ API الآن..." -ForegroundColor White
Start-ScheduledTask -TaskName $TaskName
Start-Sleep -Seconds 6

# ── 5) التحقق ───────────────────────────────────────────────────
Write-Host "[4/4] التحقق من الاتصال...`n" -ForegroundColor White

$ok = $false
for ($i = 1; $i -le 10; $i++) {
    try {
        $r = Invoke-WebRequest "http://localhost:5034/api/Items" -TimeoutSec 4 -UseBasicParsing
        Write-Host "   ✅ الـ API شغال — HTTP $($r.StatusCode)" -ForegroundColor Green
        Write-Host "   المحتوى: $($r.Content)" -ForegroundColor Gray
        $ok = $true
        break
    } catch {
        Write-Host "   ...محاولة $i — لسه بيثبت ($($_.Exception.Message))" -ForegroundColor DarkGray
        Start-Sleep -Seconds 3
    }
}

Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
if ($ok) {
    Write-Host "  ✅ تم — الـ API دلوقتي بيبدأ لوحده مع الجهاز" -ForegroundColor Green
} else {
    Write-Host "  ⚠️ الـ API محتاج وقت أطول — افتح:" -ForegroundColor Yellow
    Write-Host "     taskschd.msc  →  المهام → $TaskName  →  تشغيل" -ForegroundColor Yellow
}
Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

Write-Host "أوامر مفيدة:" -ForegroundColor Gray
Write-Host "  Start-ScheduledTask -TaskName '$TaskName'   # تشغيل" -ForegroundColor Gray
Write-Host "  Stop-ScheduledTask  -TaskName '$TaskName'   # إيقاف" -ForegroundColor Gray
Write-Host "  Get-ScheduledTask   -TaskName '$TaskName'   # حالة" -ForegroundColor Gray
