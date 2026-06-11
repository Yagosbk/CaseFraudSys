# Verifica cobertura mesclada (unit + integração) com meta de 80%.
# Requer DynamoDB Local em http://localhost:8000 para testes de integração.

param(
    [double]$Threshold = 80
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if (Test-Path "$root/TestResults") {
    Remove-Item -Recurse -Force "$root/TestResults"
}
New-Item -ItemType Directory -Path "$root/TestResults" | Out-Null

Write-Host "Running unit tests with coverage..."
dotnet test "$root/tests/CaseFraudSys.Api.Tests" `
    --filter "Category=Unit" `
    --collect:"XPlat Code Coverage" `
    --settings "$root/tests/coverlet.runsettings" `
    --results-directory "$root/TestResults"

Write-Host "Running integration tests with coverage..."
dotnet test "$root/tests/CaseFraudSys.Api.IntegrationTests" `
    --filter "Category=Integration" `
    --collect:"XPlat Code Coverage" `
    --settings "$root/tests/coverlet.runsettings" `
    --results-directory "$root/TestResults"

Write-Host "Merging coverage reports..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null

$coverageFiles = @(Get-ChildItem -Path "$root/TestResults" -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue)
if ($coverageFiles.Count -eq 0) {
    Write-Error "Nenhum arquivo coverage.cobertura.xml encontrado em TestResults. Execute os testes com --collect:`"XPlat Code Coverage`" primeiro."
}

$reportsArg = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
$targetDir = Join-Path $root "coveragereport"

# Use reportgenerator (ferramenta global), NÃO "dotnet reportgenerator"
& reportgenerator "-reports:$reportsArg" "-targetdir:$targetDir" "-reporttypes:TextSummary" "-assemblyfilters:+CaseFraudSys.Api"

$summary = Get-Content (Join-Path $targetDir "Summary.txt") -Raw
Write-Host $summary

if ($summary -match 'Line coverage: ([0-9.]+)%') {
    $lineCoverage = [double]$Matches[1]
    if ($lineCoverage -lt $Threshold) {
        Write-Error "Line coverage ${lineCoverage}% is below threshold ${Threshold}%"
    }
    Write-Host "Coverage OK: ${lineCoverage}% >= ${Threshold}%"
}
else {
    Write-Error "Could not parse line coverage from Summary.txt"
}
