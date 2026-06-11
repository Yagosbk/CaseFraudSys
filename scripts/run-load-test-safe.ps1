# Executa NBomber com perfil reduzido e encerra se o uso de memória passar do limite.
param(
    [long]$MaxMemoryMb = 750,
    [int]$PollIntervalSeconds = 2
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$loadTestDir = Join-Path $root "tests\CaseFraudSys.LoadTests"
$binDir = Join-Path $loadTestDir "bin\Debug\net8.0"
$appsettingsPath = Join-Path $binDir "appsettings.json"
$backupPath = "$appsettingsPath.bak"

$smokeSettings = @'
{
  "LoadTest": {
    "BaseUrl": "http://localhost:5067",
    "Health": { "Rate": 20, "DurationSeconds": 5 },
    "GetLimit": { "Agency": "0001", "Account": "12345", "Rate": 20, "DurationSeconds": 5 },
    "PixConcurrent": {
      "Agency": "0001",
      "Account": "99999",
      "Document": "11122233344",
      "InitialLimit": 100000.00,
      "Copies": 10,
      "Iterations": 50,
      "Amount": 10.00
    }
  }
}
'@

Write-Host "Verificando API em http://localhost:5067/api/health ..."
try {
    $health = Invoke-WebRequest -Uri "http://localhost:5067/api/health" -UseBasicParsing -TimeoutSec 5
    if ($health.StatusCode -ne 200) { throw "API retornou $($health.StatusCode)" }
}
catch {
    Write-Error "API indisponível. Suba DynamoDB + API antes: cd src\CaseFraudSys.Api; dotnet run"
}

Write-Host "Compilando load tests..."
dotnet build "$loadTestDir" -v q | Out-Null

if (Test-Path $appsettingsPath) {
    Copy-Item $appsettingsPath $backupPath -Force
}
Set-Content -Path $appsettingsPath -Value $smokeSettings -Encoding UTF8

Write-Host "Perfil: smoke (Health 5s, GET 5s, PIX 50 tx) | Limite memória: ${MaxMemoryMb} MB"
Write-Host ""

$process = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--no-build" `
    -WorkingDirectory $loadTestDir `
    -PassThru `
    -NoNewWindow

$maxBytes = $MaxMemoryMb * 1MB
$stoppedByMemory = $false

function Get-ProcessTreeIds([int]$rootPid) {
    $queue = [System.Collections.Generic.Queue[int]]::new()
    $queue.Enqueue($rootPid)
    $visited = @{}

    while ($queue.Count -gt 0) {
        $procId = $queue.Dequeue()
        if ($visited.ContainsKey($procId)) { continue }
        $visited[$procId] = $true

        Get-CimInstance Win32_Process -Filter "ParentProcessId=$procId" -ErrorAction SilentlyContinue |
            ForEach-Object { $queue.Enqueue($_.ProcessId) }
    }

    return $visited.Keys
}

function Get-ProcessTreeMemory([int]$rootPid) {
    $total = 0L
    foreach ($procId in (Get-ProcessTreeIds -rootPid $rootPid)) {
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if ($proc) { $total += $proc.WorkingSet64 }
    }
    return $total
}

function Stop-ProcessTree([int]$rootPid) {
    foreach ($procId in (Get-ProcessTreeIds -rootPid $rootPid)) {
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
}

try {
    while (-not $process.HasExited) {
        Start-Sleep -Seconds $PollIntervalSeconds

        $memBytes = Get-ProcessTreeMemory -rootPid $process.Id
        $memMb = [math]::Round($memBytes / 1MB, 1)
        Write-Host "[monitor] Memória NBomber: ${memMb} MB / ${MaxMemoryMb} MB"

        if ($memBytes -gt $maxBytes) {
            Write-Warning "Limite de memória excedido (${memMb} MB). Encerrando teste..."
            Stop-ProcessTree -rootPid $process.Id
            $stoppedByMemory = $true
            break
        }
    }
}
finally {
    if (-not $process.HasExited) {
        $process.WaitForExit(10000)
    }

    if (Test-Path $backupPath) {
        Move-Item $backupPath $appsettingsPath -Force
    }
}

Write-Host ""
if ($stoppedByMemory) {
    Write-Error "Teste interrompido por uso excessivo de memória (limite: ${MaxMemoryMb} MB)."
    exit 2
}

Write-Host "Teste concluído. Exit code: $($process.ExitCode)"
Write-Host "Relatório em: tests\CaseFraudSys.LoadTests\reports\"
exit $process.ExitCode
