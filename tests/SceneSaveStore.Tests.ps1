param([string]$WorkDirectory = [IO.Path]::GetTempPath(), [string]$JavaHome = $env:JAVA_HOME)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $JavaHome -and $env:JAVA_HOME_17_X64) { $JavaHome = $env:JAVA_HOME_17_X64 }
$javac = if ($JavaHome) { Join-Path $JavaHome 'bin/javac' } else { 'javac' }
$java = if ($JavaHome) { Join-Path $JavaHome 'bin/java' } else { 'java' }
if (-not (Get-Command $javac -ErrorAction SilentlyContinue) -or -not (Get-Command $java -ErrorAction SilentlyContinue)) {
    throw 'JDK 17 ou superior nao encontrado para testar os saves. Informe JAVA_HOME ou -JavaHome.'
}
$fixture = Join-Path $WorkDirectory ('minepeak-scenes-test-' + [guid]::NewGuid().ToString('N'))
$classes = Join-Path $fixture 'classes'
New-Item -ItemType Directory -Path $classes -Force | Out-Null
$source = Join-Path $PSScriptRoot '../coop-fabric/src/client/java/dev/skycraft/client/scene/SceneSaveStore.java'
$backupSource = Join-Path $PSScriptRoot '../coop-fabric/src/client/java/dev/skycraft/client/scene/SceneBackupVerifier.java'
& $javac --release 17 -encoding UTF-8 -d $classes $source $backupSource (Join-Path $PSScriptRoot 'SceneSaveStoreTests.java')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes do diario de saves.' }
& $java -cp $classes SceneSaveStoreTests (Join-Path $fixture 'worlds')
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes do diario de saves.' }
exit 0
