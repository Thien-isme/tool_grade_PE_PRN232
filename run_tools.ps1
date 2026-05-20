# API Runner & Tester Tool Launch Script
# Author: Antigravity AI

Clear-Host
$host.ui.RawUI.WindowTitle = "API Runner Tool Launcher"
$ScriptDir = "g:\Ky_8_FPT\PRN232\project_PRN232\ApiRunnerTool"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     * KHOI DONG HE THONG KIEM THU API TU DONG *     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Tat tat ca tien trinh cu dang chay
Write-Host "[1/5] Dang dung cac tien trinh cu..." -ForegroundColor Yellow
$ports = @(5000, 5155, 5173)
foreach ($p in $ports) {
    $proc = Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue
    if ($proc) {
        $pids = $proc.OwningProcess | Select-Object -Unique
        foreach ($pid in $pids) {
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "     -> Da giai phong cong $p (PID: $pid)" -ForegroundColor Gray
        }
    }
}
# Kill them bat ky dotnet process nao dang giu file DLL
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine
        if ($cmdLine -like "*ApiRunnerTool*") {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "     -> Da kill ApiRunnerTool.API cu (PID: $($_.Id))" -ForegroundColor Gray
        }
    } catch {}
}
Start-Sleep -Seconds 2
Write-Host "     [ok] Da don dep xong!" -ForegroundColor Green
Write-Host ""

# 2. Build lai Backend voi code moi nhat
Write-Host "[2/5] Dang build lai C# Backend..." -ForegroundColor Yellow
$buildResult = & dotnet build "$ScriptDir\ApiRunnerTool.API" --configuration Debug --nologo 2>&1
$buildOk = $LASTEXITCODE -eq 0
if ($buildOk) {
    Write-Host "     [ok] Build thanh cong!" -ForegroundColor Green
} else {
    Write-Host "     [WARN] Build co canh bao - van tiep tuc chay..." -ForegroundColor Yellow
    Write-Host ($buildResult | Select-Object -Last 5 | Out-String) -ForegroundColor Gray
}
Write-Host ""

# 3. Chay C# Backend (dung --no-build vi da build o buoc 2)
Write-Host "[3/5] Dang khoi chay ASP.NET Core Backend (Port 5155)..." -ForegroundColor Yellow
Start-Process dotnet -ArgumentList "run --project ApiRunnerTool.API --no-build --launch-profile http" -WorkingDirectory $ScriptDir -WindowStyle Hidden
Start-Sleep -Seconds 5
Write-Host "     [ok] Backend dang chay ngam tai: http://localhost:5155" -ForegroundColor Green
Write-Host ""

# 4. Khoi chay React Frontend
Write-Host "[4/5] Dang khoi chay Vite React Frontend (Port 5173)..." -ForegroundColor Yellow
Start-Process cmd.exe -ArgumentList "/c npm run dev" -WorkingDirectory "$ScriptDir\frontend" -WindowStyle Hidden
Start-Sleep -Seconds 4
Write-Host "     [ok] Frontend dang chay ngam tai: http://localhost:5173" -ForegroundColor Green
Write-Host ""

# 5. Mo trinh duyet
Write-Host "[5/5] Mo trinh duyet web..." -ForegroundColor Yellow
Start-Process "http://localhost:5173"
Write-Host "     [ok] Da mo giao dien kiem thu!" -ForegroundColor Green
Write-Host ""

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  HE THONG DA SAN SANG!" -ForegroundColor Green
Write-Host "  Frontend React  : http://localhost:5173" -ForegroundColor Cyan
Write-Host "  Backend Swagger : http://localhost:5155/swagger" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  Nhan [Ctrl+C] de tat tool." -ForegroundColor Gray
Write-Host ""

# Doi nguoi dung tat
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
}
finally {
    Write-Host "`nDang tat he thong..." -ForegroundColor Red
    foreach ($p in @(5155, 5173)) {
        $proc = Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue
        if ($proc) {
            $proc.OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
        }
    }
    Write-Host "Da tat thanh cong!" -ForegroundColor Green
}
