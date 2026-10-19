Add-Type -AssemblyName System

# توليد صوت "بيب" قصير للمسح (WAV — PCM 16-bit mono 44.1kHz)
$sampleRate = 44100
$frequency  = 1400        # تردد البيب (حاد ومسموع)
$duration   = 0.13        # مدة الصوت بالثانية
$amplitude  = 0.55
$fade       = 0.01        # تلاشي بسيط لمنع الطقطقة

$samples = [int]($sampleRate * $duration)
$fadeIn   = [int]($sampleRate * $fade)
$fadeOut  = [int]($sampleRate * $fade)

$data = New-Object byte[] ($samples * 2)   # 16-bit = 2 بايت لكل عينة

for ($i = 0; $i -lt $samples; $i++) {
    $env = 1.0
    if ($i -lt $fadeIn)         { $env = $i / [double]$fadeIn }
    elseif ($i -gt ($samples - $fadeOut)) { $env = ($samples - $i) / [double]$fadeOut }

    $val = [Math]::Sin(2 * [Math]::PI * $frequency * ($i / [double]$sampleRate)) * $amplitude * $env
    $pcm = [int16]([Math]::Round($val * 32767))
    $bytes = [BitConverter]::GetBytes($pcm)
    $data[$i * 2]     = $bytes[0]
    $data[$i * 2 + 1] = $bytes[1]
}

# ===== ترويسة WAV =====
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

$byteRate   = $sampleRate * 2 * 1
$blockAlign = 2

$bw.Write([char[]]"RIFF")
$bw.Write([int](36 + $data.Length))
$bw.Write([char[]]"WAVE")

$bw.Write([char[]]"fmt ")
$bw.Write([int](16))                 # حجم fmt
$bw.Write([int16](1))                # PCM
$bw.Write([int16](1))                # قناة واحدة
$bw.Write([int]($sampleRate))
$bw.Write([int]($byteRate))
$bw.Write([int16]($blockAlign))
$bw.Write([int16](16))               # 16-bit

$bw.Write([char[]]"data")
$bw.Write([int]($data.Length))
$bw.Write($data)

$bw.Flush()
$out = $ms.ToArray()
$bw.Dispose(); $ms.Dispose()

$dir = "ErpMobile\Resources\Raw"
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
[System.IO.File]::WriteAllBytes("$dir\beep.wav", $out)

Write-Output "saved: $dir\beep.wav  ($($out.Length) bytes)"
