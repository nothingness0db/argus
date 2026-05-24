param(
    [string]$Icon = "",
    [switch]$Run
)

$env:DOTNET_ROOT = "E:\scoop\apps\dotnet-sdk\current"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"

Write-Host "Using SDK: $(dotnet --version)"

if ($Icon -and (Test-Path $Icon)) {
    Write-Host "Converting icon: $Icon"
    python -c @"
from PIL import Image; img=Image.open(r'$Icon').convert('RGBA')
img.save(r'Assets\app.ico',format='ICO',sizes=[(16,16),(32,32),(48,48),(256,256)])
print('Icon saved')
"@
}

if ($Run) {
    taskkill /F /IM HotspotManager.exe 2>$null
    Start-Sleep -Seconds 1
}

dotnet build -c Release

if ($LASTEXITCODE -eq 0 -and $Run) {
    Write-Host "`nBuild OK. Launching..."
    $exe = Get-ChildItem -Path "bin" -Recurse -Filter "HotspotManager.exe" |
        Where-Object { $_.FullName -match "Release" } |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($exe) { Start-Process $exe.FullName } else { Write-Host "No exe found" }
} elseif ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild OK. Pass -Run to launch."
}
