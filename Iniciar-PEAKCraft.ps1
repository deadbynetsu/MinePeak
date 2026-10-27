param([switch]$Teste)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(Get-Process -Name PEAK -ErrorAction SilentlyContinue){throw 'PEAK ja esta aberto. Feche o jogo antes de iniciar esta integracao.'}
$c=Get-Content (Join-Path $root 'local-config.json') -Raw | ConvertFrom-Json
$env:JAVA_HOME=$c.java
$env:GRADLE_USER_HOME=$c.gradle
$configName=if($c.flavor -eq 'coop'){'PeakCraftCoop.cfg'}else{'aeironnarmiento.PeakCraft.cfg'}
$cfg=Join-Path $c.profile ('BepInEx\config\'+$configName)
$harness='false';if($Teste){$harness='true'}
$text='';if(Test-Path $cfg){$text=Get-Content $cfg -Raw}
if($text -match '(?m)^Harness\s*='){$text=[regex]::Replace($text,'(?m)^Harness\s*=.*$',"Harness = $harness")}
elseif($text -match '\[Dev\]'){$text=$text.Replace('[Dev]',"[Dev]`r`nHarness = $harness")}
else{$text+="`r`n[Dev]`r`nHarness = $harness`r`n"}
if($text -match '(?m)^CoopLoopback\s*='){$text=[regex]::Replace($text,'(?m)^CoopLoopback\s*=.*$','CoopLoopback = false')}
$text | Set-Content $cfg -Encoding utf8
$log=Join-Path $c.profile 'minecraft-console.log'
Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$root\Iniciar-Minecraft.ps1`"" -WindowStyle Hidden -RedirectStandardOutput $log -RedirectStandardError ($log+'.err')
$target=Join-Path $c.profile 'BepInEx\core\BepInEx.Preloader.dll'
Start-Process -FilePath $c.steam -ArgumentList "-applaunch 3527290 --doorstop-enabled true --doorstop-target-assembly `"$target`" -force-d3d11 -screen-fullscreen 0 -screen-width 1280 -screen-height 720" -WindowStyle Hidden
Write-Host 'PEAK e Minecraft iniciando. Esc abre o botao Modo Minecraft; F8 alterna.'
