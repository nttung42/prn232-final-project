# Tạo ảnh minh họa PNG riêng cho từng cơ sở demo (sân cầu lông vẽ bằng GDI+, có tên cơ sở).
# Đầu vào: file TSV UTF-8, mỗi dòng "id|tên cơ sở|quận". Đầu ra: <thư mục>\<id>.png
# Dùng: powershell -File scripts/make-court-images.ps1 -ListFile <list.tsv> -OutDir <dir>
param(
    [Parameter(Mandatory)][string]$ListFile,
    [Parameter(Mandatory)][string]$OutDir
)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$palettes = @(
    @('#0E5E57', '#F3F7F6'), @('#1B4D7A', '#EEF4FA'), @('#6B3E26', '#FBF5EF'),
    @('#2F5D3A', '#F1F7F0'), @('#5A2D6E', '#F8F3FB'), @('#7A3B1E', '#FDF4EE'),
    @('#1F4E5F', '#EFF6F8'), @('#4A4A1F', '#F7F7EE'), @('#0F4C81', '#EDF3FA')
)

$lines = Get-Content -LiteralPath $ListFile -Encoding UTF8 | Where-Object { $_ -match '\|' }
$i = 0
foreach ($line in $lines) {
    $parts = $line.Split('|')
    $id = $parts[0].Trim(); $name = $parts[1].Trim(); $district = $parts[2].Trim()
    $pal = $palettes[$i % $palettes.Count]
    $bg = [System.Drawing.ColorTranslator]::FromHtml($pal[1])
    $court = [System.Drawing.ColorTranslator]::FromHtml($pal[0])

    $bmp = New-Object System.Drawing.Bitmap 1200, 675
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear($bg)

    # Mặt sân và các vạch
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 6
    $g.FillRectangle((New-Object System.Drawing.SolidBrush $court), 150, 110, 900, 420)
    $g.DrawRectangle($pen, 150, 110, 900, 420)
    $g.DrawLine($pen, 600, 110, 600, 530)
    $g.DrawLine($pen, 150, 320, 1050, 320)
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 3), 230, 170, 740, 300)
    # Lưới
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))), 596, 100, 8, 440)

    # Chữ
    # Chữ căn giữa, nằm trong vùng giữa ảnh để không bị cắt khi thẻ hiển thị ảnh theo tỷ lệ khác
    $title = New-Object System.Drawing.Font 'Segoe UI', 34, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $sub = New-Object System.Drawing.Font 'Segoe UI', 24, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
    $ink = [System.Drawing.ColorTranslator]::FromHtml($pal[0])
    $center = New-Object System.Drawing.StringFormat
    $center.Alignment = 'Center'
    $center.LineAlignment = 'Near'
    $titleRect = New-Object System.Drawing.RectangleF 150, 552, 900, 60
    $g.DrawString($name, $title, (New-Object System.Drawing.SolidBrush $ink), $titleRect, $center)
    $subRect = New-Object System.Drawing.RectangleF 150, 612, 900, 40
    $g.DrawString("Quận $district  ·  Ảnh minh họa demo", $sub, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 0, 0, 0))), $subRect, $center)

    $out = Join-Path $OutDir "$id.png"
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    $i++
}
Write-Host "Đã tạo $i ảnh trong $OutDir"
