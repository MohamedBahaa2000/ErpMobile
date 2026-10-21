# Scan pages from a WIA scanner and combine them into a single PDF.
# Usage: powershell -File tools\scan_to_pdf.ps1 -OutFile "D:\ErpMobile\scan.pdf" -Pages 2
param(
    [string]$OutFile = "$env:USERPROFILE\Desktop\scan.pdf",
    [int]$Pages = 2,
    [string]$DeviceId = "",
    [int]$Dpi = 200,
    [string]$Color = "RGB",   # RGB | GRAY
    [string]$TmpDir = "$env:TEMP\opencode\scan"
)
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $TmpDir | Out-Null

function Get-ScannerDevice {
    param([string]$Id)
    $dm = New-Object -ComObject WIA.DeviceManager
    $list = @($dm.DeviceInfos)
    if ($list.Count -eq 0) { throw "No WIA scanners found. Check that the scanner is switched on and connected." }
    if ($Id) { $d = $list | Where-Object { $_.DeviceID -eq $Id }; if (-not $d) { throw "Device not found: $Id" }; return $d }
    # Prefer a scanner-type device ( scanner = 1 )
    $s = $list | Where-Object { $_.Type -eq 1 } | Select-Object -First 1
    if (-not $s) { $s = $list | Select-Object -First 1 }
    return $s
}

$dev = Get-ScannerDevice -Id $DeviceId
Write-Host ("Scanner: " + $dev.Properties('Name').Value)

$wia = New-Object -ComObject WIA.ImageDialog
$images = @()
for ($i = 1; $i -le $Pages; $i++) {
    Write-Host ("Scanning page $i of $Pages ...")
    $common = New-Object -ComObject WIA.CommonDialog
    $item = $common.ShowTransfer($dev, "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}", $false)  # wiaFormatPNG
    $path = Join-Path $TmpDir ("page_{0:d2}.png" -f $i)
    $item.ActiveFrame = 1
    $item.SaveFile($path)
    $images += $path
    Write-Host ("  saved " + $path)
}

# Merge into one PDF using Pillow
$py = Join-Path $TmpDir "merge.py"
@'
import sys
from PIL import Image

out = sys.argv[1]
paths = sys.argv[2:]
imgs = []
for p in paths:
    im = Image.open(p)
    if im.mode != "RGB":
        im = im.convert("RGB")
    imgs.append(im)
imgs[0].save(out, "PDF", save_all=True, append_images=imgs[1:], resolution=150.0)
print("WROTE", out, len(imgs), "pages")
'@ | Set-Content -Path $py -Encoding UTF8

$python = "python"
& $python $py $OutFile $images
if ($LASTEXITCODE -ne 0) { throw "PDF creation failed" }
Write-Host ("PDF ready: " + $OutFile)
