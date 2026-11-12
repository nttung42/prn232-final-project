# Chạy MinIO (object storage S3-compatible) cho dự án local.
# - Sinh khóa ngẫu nhiên lần đầu và ghi vào .env (đã gitignore), không in ra màn hình.
# - Tạo bucket công khai đọc để ảnh cơ sở hiển thị được qua URL.
# Dùng: powershell -File scripts/minio.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env'
$container = 'courtside-minio'
$volume = 'courtside-minio-data'
$bucket = 'courtside-uploads'
$minioImage = 'bitnamilegacy/minio:latest'
$mcImage = 'bitnamilegacy/minio-client:latest'

function New-Secret([int]$bytes) {
    $b = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    return ([Convert]::ToBase64String($b) -replace '[^A-Za-z0-9]', '').Substring(0, [Math]::Min(24, $bytes))
}

# 1. Đọc .env hiện có, thêm khóa MinIO nếu chưa có
$lines = if (Test-Path $envFile) { Get-Content -LiteralPath $envFile } else { @() }
$keys = @{}
foreach ($l in $lines) { if ($l -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') { $keys[$matches[1]] = $matches[2] } }

$rootUser = if ($keys['MINIO_ROOT_USER']) { $keys['MINIO_ROOT_USER'] } else { 'courtside' }
$rootPass = if ($keys['MINIO_ROOT_PASSWORD']) { $keys['MINIO_ROOT_PASSWORD'] } else { New-Secret 24 }

$add = @{
    'MINIO_ROOT_USER'                 = $rootUser
    'MINIO_ROOT_PASSWORD'             = $rootPass
    'CloudflareR2__AccountId'         = 'local'
    'CloudflareR2__AccessKeyId'       = $rootUser
    'CloudflareR2__SecretAccessKey'   = $rootPass
    'CloudflareR2__ServiceUrl'        = 'http://localhost:9000'
    'CloudflareR2__BucketName'        = $bucket
    'CloudflareR2__PublicDomain'      = "http://localhost:9000/$bucket"
}
$toAppend = @()
foreach ($k in $add.Keys) { if (-not $keys.ContainsKey($k)) { $toAppend += "$k=$($add[$k])" } }
if ($toAppend.Count -gt 0) { Add-Content -LiteralPath $envFile -Value $toAppend -Encoding utf8 }
Write-Host "Đã cập nhật .env ($($toAppend.Count) khóa mới)."

# 2. Chạy container MinIO
$running = docker ps --filter "name=^$container$" --format '{{.Names}}'
if (-not $running) {
    docker rm -f $container 2>$null | Out-Null
    docker volume create $volume | Out-Null
    docker run -d --name $container -p 9000:9000 -p 9001:9001 `
        -e "MINIO_ROOT_USER=$rootUser" -e "MINIO_ROOT_PASSWORD=$rootPass" `
        -e "MINIO_DEFAULT_BUCKETS=$bucket" `
        -v "${volume}:/bitnami/minio/data" $minioImage | Out-Null
    Write-Host "Đã khởi động MinIO (API: 9000, Console: 9001)."
} else {
    Write-Host "MinIO đang chạy."
}

# 3. Đợi server sẵn sàng rồi đặt bucket đọc công khai (ảnh hiển thị được qua URL)
$ready = $false
for ($i = 0; $i -lt 30 -and -not $ready; $i++) {
    try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:9000/minio/health/live' -TimeoutSec 3; $ready = ($r.StatusCode -eq 200) } catch { Start-Sleep -Seconds 2 }
}
if (-not $ready) { throw 'MinIO chưa sẵn sàng sau 60 giây.' }

$cmd = "mc alias set local http://host.docker.internal:9000 '$rootUser' '$rootPass' >/dev/null && mc mb --ignore-existing local/$bucket && mc anonymous set download local/$bucket && mc anonymous get local/$bucket"
docker run --rm --add-host host.docker.internal:host-gateway --entrypoint sh $mcImage -c $cmd
if ($LASTEXITCODE -ne 0) { throw "Không đặt được quyền đọc công khai cho bucket '$bucket' (mã lỗi $LASTEXITCODE)." }
Write-Host "MinIO san sang. Bucket '$bucket' cho phep doc cong khai."
