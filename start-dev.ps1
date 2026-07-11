[CmdletBinding()]
param(
    # 后端端口须与 IotWebApi/appsettings.json 的 "Urls" 一致（Program.cs UseUrls 以配置为准，改这里只影响端口释放/提示）
    [int]$ApiPort = 13699,
    [int]$WebPort = 8868,
    [switch]$SkipInstall,
    [switch]$OpenBrowser,
    # 默认每次重启强制重编译；加 -NoRebuild 显式跳过（仅紧急场景用）
    [switch]$NoRebuild,
    # 跳过前端 dist / vite 缓存清理（HMR 改 .vue 时一般不需要）
    [switch]$NoCleanWeb
)

$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Ok {
    param([string]$Message)
    Write-Host "[OK]   $Message" -ForegroundColor Green
}

function Write-Err {
    param([string]$Message)
    Write-Host "[ERR]  $Message" -ForegroundColor Red
}

function Test-PortInUse {
    param([int]$Port)
    try {
        $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop
        return $null -ne $conn
    }
    catch {
        return $false
    }
}

# 杀掉占用指定端口的所有进程（端口被占不跳过 → 强制释放）
function Stop-PortOwner {
    param([int]$Port, [string]$Label)
    try {
        $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop
    }
    catch {
        return
    }
    if (-not $conns) { return }

    $pids = $conns | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $pids) {
        if (-not $procId -or $procId -eq 0) { continue }
        try {
            $p = Get-Process -Id $procId -ErrorAction Stop
            Write-Warn "释放 $Label 端口 $Port — 杀掉 PID=$procId（$($p.ProcessName)）"
            Stop-Process -Id $procId -Force -ErrorAction Stop
        }
        catch {
            Write-Warn "杀 PID=$procId 失败：$($_.Exception.Message)（可能已退出）"
        }
    }

    # 等端口真正释放（最多 5 秒）
    $waited = 0
    while ((Test-PortInUse -Port $Port) -and $waited -lt 5000) {
        Start-Sleep -Milliseconds 250
        $waited += 250
    }
    if (Test-PortInUse -Port $Port) {
        throw "$Label 端口 $Port 释放超时（5 秒），请手动杀掉对应进程后重试。"
    }
    Write-Ok "$Label 端口 $Port 已释放"
}

function Resolve-ShellExe {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) { return $pwshCmd.Source }

    $powershellCmd = Get-Command powershell -ErrorAction SilentlyContinue
    if ($powershellCmd) { return $powershellCmd.Source }

    throw "未找到 pwsh 或 powershell，请先安装 PowerShell。"
}

function Start-ServiceWindow {
    param(
        [string]$ShellExe,
        [string]$Title,
        [string]$WorkingDirectory,
        [string]$RunCommand
    )

    $escapedCd = $WorkingDirectory.Replace("'", "''")
    $fullCommand = "Set-Location '$escapedCd'; `$Host.UI.RawUI.WindowTitle = '$Title'; $RunCommand"

    Start-Process -FilePath $ShellExe -ArgumentList @("-NoExit", "-Command", $fullCommand) | Out-Null
}

$repoRoot = Split-Path -Parent $PSCommandPath
$webApiDir = Join-Path $repoRoot "IotWebApi"
$webDir = Join-Path $repoRoot "vuefrontend"

if (-not (Test-Path $webApiDir)) {
    throw "未找到目录: $webApiDir"
}

if (-not (Test-Path $webDir)) {
    throw "未找到目录: $webDir"
}

$shellExe = Resolve-ShellExe

Write-Info "项目根目录: $repoRoot"
Write-Info "Shell: $shellExe"
if ($NoRebuild) {
    Write-Warn "已传入 -NoRebuild：跳过强制重编译（仅启动）"
}
else {
    Write-Info "模式: 每次启动强制重编译（如需跳过传 -NoRebuild）"
}
Write-Warn "依赖三件套须已在 WSL docker 中运行：PostgreSQL(6305) / Redis(8111) / RabbitMQ MQTT(1883)"

if (-not $SkipInstall) {
    Write-Info "检查前端依赖..."
    $nodeModules = Join-Path $webDir "node_modules"
    if (-not (Test-Path $nodeModules)) {
        Write-Info "未检测到 node_modules，执行 pnpm install"
        Push-Location $webDir
        try {
            & pnpm install
            if ($LASTEXITCODE -ne 0) {
                throw "pnpm install 失败，退出码: $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }
        Write-Ok "前端依赖安装完成"
    }
    else {
        Write-Ok "已存在 node_modules，跳过安装"
    }
}
else {
    Write-Warn "已按参数跳过依赖安装"
}

# ========== 强制释放端口（不"端口占用则跳过"）==========
Stop-PortOwner -Port $ApiPort -Label "后端"
Stop-PortOwner -Port $WebPort -Label "前端"

# ========== 强制重编译（除非 -NoRebuild）==========
# 注意：后端固定 Release 配置——运行时配置 DbSetting.config（PG/Redis 连接）落在
# bin/Release/net10.0/Config/ 下，Debug 输出会重新生成 MySQL 默认配置导致连库失败
if (-not $NoRebuild) {
    Write-Info "[1/2] 强制重编译后端：dotnet build IotWebApi (Release)"
    Push-Location $repoRoot
    try {
        & dotnet build (Join-Path $webApiDir "IotWebApi.csproj") -c Release --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Err "后端编译失败（exit=$LASTEXITCODE），已中止启动 — 修好编译错误再重试。"
            exit $LASTEXITCODE
        }
    }
    finally {
        Pop-Location
    }
    Write-Ok "后端编译通过"

    if (-not $NoCleanWeb) {
        Write-Info "[2/2] 清理前端 dist + Vite 缓存（确保拉到最新代码）"
        $distDir = Join-Path $webDir "dist"
        $viteCache = Join-Path $webDir "node_modules\.vite"
        foreach ($p in @($distDir, $viteCache)) {
            if (Test-Path $p) {
                try {
                    Remove-Item -Path $p -Recurse -Force -ErrorAction Stop
                    Write-Ok "已删除: $p"
                }
                catch {
                    Write-Warn "删除 $p 失败：$($_.Exception.Message)（可能被占用，将继续）"
                }
            }
        }
    }
    else {
        Write-Warn "已按 -NoCleanWeb 跳过前端缓存清理"
    }
}

$apiUrl = "http://127.0.0.1:$ApiPort"
$webUrl = "http://127.0.0.1:$WebPort"

# 后端：Production 环境 + Release 产物 + --no-launch-profile（launchSettings 会强制 Development/15089）
# 端口由 appsettings.json 的 "Urls"(http://*:13699) 决定
if ($NoRebuild) {
    $apiCmd = "`$env:ASPNETCORE_ENVIRONMENT='Production'; dotnet run --project ./IotWebApi -c Release --no-launch-profile"
}
else {
    # 用 --no-build 直接跑刚编译出的产物（避免 dotnet run 重复编译）
    $apiCmd = "`$env:ASPNETCORE_ENVIRONMENT='Production'; dotnet run --project ./IotWebApi -c Release --no-build --no-launch-profile"
}
Start-ServiceWindow -ShellExe $shellExe -Title "Zxx.Iot Backend :$ApiPort" -WorkingDirectory $repoRoot -RunCommand $apiCmd
Write-Ok "后端启动命令已发送（端口 $ApiPort）"

# 前端：Vite dev 自带 HMR，但已清缓存确保拉到最新代码（.env.development VITE_PORT=8868，--port 显式对齐）
$webCmd = "pnpm --dir ./vuefrontend dev --host 127.0.0.1 --port $WebPort"
Start-ServiceWindow -ShellExe $shellExe -Title "Zxx.Iot Frontend :$WebPort" -WorkingDirectory $repoRoot -RunCommand $webCmd
Write-Ok "前端启动命令已发送（端口 $WebPort）"

Write-Host ""
Write-Host "访问地址:" -ForegroundColor White
Write-Host "前端: $webUrl" -ForegroundColor White
Write-Host "后端: $apiUrl" -ForegroundColor White
Write-Host "登录: superadmin / Admin666" -ForegroundColor White
Write-Host ""
Write-Host "提示：" -ForegroundColor DarkGray
Write-Host "  - 跳过编译：start-dev.ps1 -NoRebuild" -ForegroundColor DarkGray
Write-Host "  - 跳过前端 clean：start-dev.ps1 -NoCleanWeb" -ForegroundColor DarkGray
Write-Host "  - 跳过 pnpm install：start-dev.ps1 -SkipInstall" -ForegroundColor DarkGray
Write-Host "  - 后端 MQTT broker 地址读库表 admin_mqttparam，改表后须重启后端" -ForegroundColor DarkGray

if ($OpenBrowser) {
    Start-Process $webUrl | Out-Null
    Write-Ok "已打开浏览器: $webUrl"
}
