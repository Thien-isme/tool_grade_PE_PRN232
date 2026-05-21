# API Runner & Tester Tool Launch Script
$ErrorActionPreference = "Continue"

Clear-Host
$host.ui.RawUI.WindowTitle = "API Runner Tool Launcher"
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) { $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }

function Stop-PortListeners {
    param([int[]]$PortList)
    foreach ($port in $PortList) {
        try {
            $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if (-not $connections) { continue }
            $processIds = $connections.OwningProcess | Select-Object -Unique
            foreach ($processId in $processIds) {
                if ($processId -le 0) { continue }
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                Write-Host "     -> Da giai phong cong $port (PID: $processId)" -ForegroundColor Gray
            }
        } catch {}
    }
}

function Test-PortOpen {
    param([int]$Port, [int]$TimeoutSec = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $c = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            if ($c) { return $true }
        } catch {}
        Start-Sleep -Seconds 1
    }
    return $false
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     * KHOI DONG HE THONG KIEM THU API TU DONG *     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Tat tien trinh cu
Write-Host "[1/5] Dang dung cac tien trinh cu..." -ForegroundColor Yellow
Stop-PortListeners -PortList @(5000, 5155, 5173)

Get-Process -Name "dotnet", "node" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)" -ErrorAction SilentlyContinue).CommandLine
        if ($cmdLine -match "ApiRunnerTool|vite|5173|tool_grade_PE_PRN232\\frontend") {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "     -> Da kill PID $($_.Id)" -ForegroundColor Gray
        }
    } catch {}
}
# Dam bao giai phong file DLL truoc khi build
Stop-PortListeners -PortList @(5155, 5173)
Start-Sleep -Seconds 4
Write-Host "     [ok] Da don dep xong!" -ForegroundColor Green
Write-Host ""

# 2. Build Backend
Write-Host "[2/5] Dang build lai C# Backend..." -ForegroundColor Yellow
$buildResult = & dotnet build "$ScriptDir\ApiRunnerTool.API\ApiRunnerTool.API.csproj" --configuration Debug --nologo 2>&1
$buildOk = $LASTEXITCODE -eq 0
if ($buildOk) {
    Write-Host "     [ok] Build thanh cong!" -ForegroundColor Green
} else {
    Write-Host "     [WARN] Build that bai - thu chay ban build cu..." -ForegroundColor Yellow
    Write-Host ($buildResult | Select-Object -Last 6 | Out-String) -ForegroundColor Gray
}
Write-Host ""

# 3. Backend
Write-Host "[3/5] Dang khoi chay ASP.NET Core Backend (Port 5155)..." -ForegroundColor Yellow
$backendArgs = if ($buildOk) {
    "run --project ApiRunnerTool.API --no-build --launch-profile http"
} else {
    "run --project ApiRunnerTool.API --launch-profile http"
}
Start-Process dotnet -ArgumentList $backendArgs -WorkingDirectory $ScriptDir -WindowStyle Hidden

if (Test-PortOpen -Port 5155 -TimeoutSec 25) {
    Write-Host "     [ok] Backend: http://localhost:5155" -ForegroundColor Green
} else {
    Write-Host "     [LOI] Backend khong len cong 5155!" -ForegroundColor Red
}
Write-Host ""

# 4. Frontend
Write-Host "[4/5] Dang khoi chay Vite React Frontend (Port 5173)..." -ForegroundColor Yellow
$frontendDir = Join-Path $ScriptDir "frontend"
# Dung npm.cmd tranh loi ExecutionPolicy chan npm.ps1 tren PowerShell
$npmCmd = Get-Command npm.cmd -ErrorAction SilentlyContinue
if (-not $npmCmd) { $npmCmd = Get-Command npm -ErrorAction SilentlyContinue }
$npmExe = if ($npmCmd) { $npmCmd.Source } else { $null }

if (-not $npmExe) {
    Write-Host "     [LOI] Chua cai Node.js/npm!" -ForegroundColor Red
    Write-Host "     -> Tai: https://nodejs.org (ban LTS)" -ForegroundColor Yellow
    Write-Host "     -> Sau khi cai, mo terminal moi va chay lai ChayAPI.bat" -ForegroundColor Yellow
    Write-Host "     -> Hoac chi dung Swagger: http://localhost:5155/swagger" -ForegroundColor Yellow
} else {
    if (-not (Test-Path (Join-Path $frontendDir "node_modules"))) {
        Write-Host "     Dang npm install (lan dau, co the mat 1-2 phut)..." -ForegroundColor Gray
        Push-Location $frontendDir
        & cmd.exe /c "`"$npmExe`" install" 2>&1 | Out-Null
        Pop-Location
    }

    $viteLog = Join-Path $ScriptDir "frontend_vite.log"
    Start-Process cmd.exe -ArgumentList "/c `"$npmExe`" run dev > `"$viteLog`" 2>&1" -WorkingDirectory $frontendDir -WindowStyle Hidden

    if (Test-PortOpen -Port 5173 -TimeoutSec 90) {
        Write-Host "     [ok] Frontend: http://localhost:5173" -ForegroundColor Green
    } else {
        Write-Host "     [LOI] Frontend khong len cong 5173 trong 90s!" -ForegroundColor Red
        Write-Host "     -> Mo them cua so: ChayFrontend.bat (de xem loi Vite)" -ForegroundColor Yellow
        if (Test-Path $viteLog) {
            Write-Host "     -> Log: $viteLog" -ForegroundColor Yellow
            Get-Content $viteLog -Tail 15 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkGray }
        }
    }
}
Write-Host ""

# 5. Mo trinh duyet
Write-Host "[5/5] Mo trinh duyet web..." -ForegroundColor Yellow
if (Test-PortOpen -Port 5173 -TimeoutSec 3) {
    Start-Process "http://localhost:5173"
    Write-Host "     [ok] Da mo http://localhost:5173" -ForegroundColor Green
} elseif (Test-PortOpen -Port 5155 -TimeoutSec 3) {
    Start-Process "http://localhost:5155/swagger"
    Write-Host "     [ok] Frontend chua chay - mo Swagger thay the" -ForegroundColor Yellow
} else {
    Write-Host "     [LOI] Khong co dich vu nao san sang!" -ForegroundColor Red
}
Write-Host ""

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  TRANG THAI HE THONG" -ForegroundColor Green
Write-Host "  Frontend React  : http://localhost:5173" -ForegroundColor Cyan
Write-Host "  Backend Swagger : http://localhost:5155/swagger" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "  Nhan [Ctrl+C] de tat tool." -ForegroundColor Gray
Write-Host ""

try {
    while ($true) { Start-Sleep -Seconds 1 }
}
finally {
    Write-Host "`nDang tat he thong..." -ForegroundColor Red
    Stop-PortListeners -PortList @(5155, 5173)
    Write-Host "Da tat thanh cong!" -ForegroundColor Green
}
