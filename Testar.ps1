param([string]$Dotnet = 'dotnet', [string]$WorkDirectory = [IO.Path]::GetTempPath(), [string]$JavaHome = $env:JAVA_HOME)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command $Dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET 10 nao encontrado. Instale-o ou informe -Dotnet com o caminho do dotnet.exe.'
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$projects = @('VitalsLedger.Tests', 'CollisionRegion.Tests', 'Multiplayer.Tests', 'InputSafety.Tests', 'SceneRenderFence.Tests')
foreach ($project in $projects) {
    $path = Join-Path $PSScriptRoot ('tests/' + $project + '/' + $project + '.csproj')
    Write-Host ('Executando testes: ' + $project)
    & $Dotnet run --project $path -c Release
    if ($LASTEXITCODE -ne 0) { throw ('Falha nos testes: ' + $project + ' (codigo ' + $LASTEXITCODE + ').') }
}
& (Join-Path $PSScriptRoot 'tests/Installer.Tests.ps1') -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes do instalador.' }
& (Join-Path $PSScriptRoot 'tests/Launcher.Tests.ps1') -WorkDirectory $WorkDirectory
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes do inicializador.' }
& (Join-Path $PSScriptRoot 'tests/SceneSaveStore.Tests.ps1') -WorkDirectory $WorkDirectory -JavaHome $JavaHome
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes do diario de saves.' }
Write-Host 'Os projetos de testes e o instalador passaram. Este resultado nao substitui uma partida em dois PCs reais.'
