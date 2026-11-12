# Nạp biến từ file .env rồi chạy WebAPI và WebClient.
param([switch]$OnlyApi)

$envFile = Join-Path $PSScriptRoot "..\.env"
if (-not (Test-Path $envFile)) { Write-Error "Chưa có file .env. Sao chép .env.example thành .env và điền giá trị."; exit 1 }

Get-Content $envFile | Where-Object { $_ -and -not $_.StartsWith("#") } | ForEach-Object {
    $key, $value = $_ -split "=", 2
    [Environment]::SetEnvironmentVariable($key.Trim(), $value.Trim(), "Process")
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Start-Process dotnet -ArgumentList "run --project `"$root\src\BadmintonBooking.WebAPI`" --no-launch-profile --urls http://localhost:6789" -WindowStyle Normal
if (-not $OnlyApi) {
    Start-Process dotnet -ArgumentList "run --project `"$root\src\BadmintonBooking.WebClient`" --no-launch-profile --urls http://localhost:3456" -WindowStyle Normal
}
Write-Host "Đã khởi động. API: http://localhost:6789  Web: http://localhost:3456"
