# DynamoDB Local sem Docker (alternativa quando WSL/Docker nao funciona)
# Uso: .\scripts\start-dynamodb-local.ps1

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $RootDir "tools\dynamodb-local"
$ZipPath = Join-Path $ToolsDir "dynamodb_local_latest.zip"
$DownloadUrl = "https://s3.us-west-2.amazonaws.com/dynamodb-local/dynamodb_local_latest.zip"
$Port = 8000

function Test-DynamoDbRunning {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port" -Method GET -TimeoutSec 2 -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

if (Test-DynamoDbRunning) {
    Write-Host "DynamoDB Local ja esta rodando em http://localhost:$Port" -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $ToolsDir)) {
    New-Item -ItemType Directory -Path $ToolsDir -Force | Out-Null
}

$jarPath = Get-ChildItem -Path $ToolsDir -Recurse -Filter "DynamoDBLocal.jar" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $jarPath) {
    Write-Host "Baixando DynamoDB Local..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -UseBasicParsing
    Expand-Archive -Path $ZipPath -DestinationPath $ToolsDir -Force
    $jarPath = Get-ChildItem -Path $ToolsDir -Recurse -Filter "DynamoDBLocal.jar" | Select-Object -First 1
}

if (-not $jarPath) {
    Write-Error "Nao foi possivel encontrar DynamoDBLocal.jar apos o download."
}

$jarDir = $jarPath.DirectoryName
$libDir = Join-Path $jarDir "DynamoDBLocal_lib"

Write-Host "Iniciando DynamoDB Local em http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Pasta: $jarDir" -ForegroundColor Gray
Write-Host "Pressione Ctrl+C para parar." -ForegroundColor Gray

Push-Location $jarDir
try {
    java "-Djava.library.path=$libDir" -jar $jarPath.FullName -sharedDb -inMemory -port $Port
} finally {
    Pop-Location
}
