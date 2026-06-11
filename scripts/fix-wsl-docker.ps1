# Execute este script como Administrador (clique direito -> Executar como administrador)
# Corrige WSL desatualizado exigido pelo Docker Desktop

$ErrorActionPreference = "Stop"

Write-Host "=== Corrigindo WSL para Docker Desktop ===" -ForegroundColor Cyan

Write-Host "`n[1/4] Atualizando WSL..." -ForegroundColor Yellow
wsl --update --web-download

Write-Host "`n[2/4] Definindo WSL 2 como padrao..." -ForegroundColor Yellow
wsl --set-default-version 2

Write-Host "`n[3/4] Instalando Ubuntu (se ainda nao existir)..." -ForegroundColor Yellow
$distros = wsl --list --quiet 2>$null
if ($distros -notmatch "Ubuntu") {
    wsl --install -d Ubuntu --no-launch
} else {
    Write-Host "Ubuntu ja instalado." -ForegroundColor Green
}

Write-Host "`n[4/4] Verificando instalacao..." -ForegroundColor Yellow
wsl --status
wsl --list --verbose

Write-Host "`n=== Concluido ===" -ForegroundColor Green
Write-Host "REINICIE o computador, abra o Docker Desktop e rode:" -ForegroundColor White
Write-Host "  cd C:\Users\yagos\Downloads\CaseFraudSys" -ForegroundColor Gray
Write-Host "  docker compose up -d" -ForegroundColor Gray
