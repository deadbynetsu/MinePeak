param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command $Dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET 10 nao encontrado. Instale-o ou informe -Dotnet com o caminho do dotnet.exe.'
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$projects = @('VitalsLedger.Tests', 'CollisionRegion.Tests', 'Multiplayer.Tests')
foreach ($project in $projects) {
    $path = Join-Path $PSScriptRoot ('tests/' + $project + '/' + $project + '.csproj')
    Write-Host ('Executando testes: ' + $project)
    & $Dotnet run --project $path -c Release
    if ($LASTEXITCODE -ne 0) { throw ('Falha nos testes: ' + $project + ' (codigo ' + $LASTEXITCODE + ').') }
}
Write-Host 'Os tres projetos de testes passaram. Este resultado nao substitui uma partida em dois PCs reais.'
