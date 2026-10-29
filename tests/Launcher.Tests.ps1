param([string]$WorkDirectory=[IO.Path]::GetTempPath())
$ErrorActionPreference='Stop'
if([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT){Write-Output 'SKIP: Windows launcher fixture'; exit 0}
$caseRoot=Join-Path $WorkDirectory ('MinePeak-launcher-'+[guid]::NewGuid().ToString('N'))
$project=Join-Path $caseRoot 'project'
$profile=Join-Path $project 'local-profile'
$java=Join-Path $caseRoot 'java'
$game=Join-Path $caseRoot 'game'
$steam=Join-Path $caseRoot 'steam.exe'
$record=Join-Path $project 'local-config.json'
$cfg=Join-Path $profile 'BepInEx/config/PeakCraftCoop.cfg'
foreach($dir in @($project,(Join-Path $profile 'BepInEx/config'),(Join-Path $profile 'BepInEx/core'),
    (Join-Path $java 'bin'),$game,(Join-Path $project 'fabric/gradle/wrapper'))){New-Item -ItemType Directory -Force -Path $dir|Out-Null}
Copy-Item (Join-Path (Split-Path $PSScriptRoot -Parent) 'Iniciar-PEAKCraft.ps1') (Join-Path $project 'Iniciar-PEAKCraft.ps1')
foreach($path in @($steam,(Join-Path $java 'bin/java.exe'),(Join-Path $game 'PEAK.exe'),
    (Join-Path $profile 'BepInEx/core/BepInEx.Preloader.dll'),(Join-Path $project 'fabric/gradle/wrapper/gradle-wrapper.jar'))){
    'fixture, never executed'|Set-Content -LiteralPath $path
}
$config=@{flavor='coop';profile=$profile;steam=$steam;java=$java;peak=$game;gradle=(Join-Path $caseRoot 'gradle')}
$fixtureState=[pscustomobject]@{launches=0;peakOpen=$false;oldSamples=0;unrelated=$false;polls=0}
function Get-Process {param($Name,$ErrorAction) if($fixtureState.peakOpen){return [pscustomobject]@{Id=999;Name='PEAK'}}}
function Get-CimInstance {param($ClassName,$Filter)
    $fixtureState.polls++
    if($fixtureState.oldSamples -gt 0){$fixtureState.oldSamples--;return [pscustomobject]@{CommandLine=('powershell -File "'+$project+'\Iniciar-Minecraft.ps1"')}}
    if($fixtureState.unrelated){return [pscustomobject]@{CommandLine='java -Dfabric.dli.config=C:\Unrelated\launch.cfg'}}
}
function Start-Sleep {param($Milliseconds)}
function Start-Process {param($FilePath,$ArgumentList,$WindowStyle,$RedirectStandardOutput,$RedirectStandardError) $fixtureState.launches++}
function Launch {param([switch]$Teste) & (Join-Path $project 'Iniciar-PEAKCraft.ps1') -Teste:$Teste}
function Check($Condition,[string]$Name){if(-not $Condition){throw ('FAIL: '+$Name)};Write-Output ('PASS: '+$Name)}
function Refused([string]$Pattern){$fixtureState.launches=0;try{Launch;return $false}catch{return $_.Exception.Message -match $Pattern -and $fixtureState.launches -eq 0}}
$saved=@{};foreach($name in @('JAVA_HOME','GRADLE_USER_HOME','TEMP','TMP')){$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try{
    Check (Refused 'nao foi preparada') 'missing installation refuses to start either game'
    '{broken'|Set-Content $record
    Check (Refused 'Nao foi possivel ler') 'invalid configuration refuses to launch'
    @{flavor='coop'}|ConvertTo-Json|Set-Content $record
    Check (Refused 'incompleto') 'incomplete configuration has a useful preparation error'
    $config|ConvertTo-Json|Set-Content $record
    Remove-Item -LiteralPath (Join-Path $java 'bin/java.exe')
    Check (Refused 'Java') 'missing Java cannot leave a standalone PEAK launch'
    'fixture'|Set-Content (Join-Path $java 'bin/java.exe')
    $fixtureState.peakOpen=$true
    Check (Refused 'PEAK ja esta aberto') 'running PEAK refuses a second integration'
    $fixtureState.peakOpen=$false
    "[Dev]`nHarness = true`nCoopLoopback = true"|Set-Content $cfg
    $fixtureState.oldSamples=2;$fixtureState.launches=0;$fixtureState.polls=0
    Launch
    Check ($fixtureState.launches -eq 2 -and $fixtureState.polls -ge 3) 'prior Minecraft wrapper must finish before another launch'
    $fixtureState.unrelated=$true;$fixtureState.launches=0
    Launch
    Check ($fixtureState.launches -eq 2) 'unrelated Java processes do not block the project'
    $fixtureState.unrelated=$false;$fixtureState.launches=0
    Launch
    $text=Get-Content $cfg -Raw
    Check ($fixtureState.launches -eq 2 -and $text -match 'Harness = false' -and $text -match 'CoopLoopback = false' -and
        $env:TEMP.StartsWith($profile,[StringComparison]::OrdinalIgnoreCase)) 'normal launch disables diagnostics and keeps temporary files in its profile'
    $fixtureState.launches=0
    Launch -Teste
    $text=Get-Content $cfg -Raw
    Check ($fixtureState.launches -eq 2 -and $text -match 'Harness = true' -and $text -match 'CoopLoopback = false') 'opted-in test mode never enables multiplayer loopback implicitly'
}finally{foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}}
Write-Output 'PASS: 9 launcher regression cases; no game or Steam executable was started'
exit 0

