param([switch]$Teste)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$hashTool=[Security.Cryptography.SHA256]::Create()
try{$launchKey=[BitConverter]::ToString($hashTool.ComputeHash([Text.Encoding]::UTF8.GetBytes($root.ToLowerInvariant()))).Replace('-','').Substring(0,24)}
finally{$hashTool.Dispose()}
$launchMutex=New-Object Threading.Mutex($false,('Local\MinePeakLaunch.'+$launchKey))
$ownsLaunchMutex=$false
try{
try{$ownsLaunchMutex=$launchMutex.WaitOne(0)}catch [Threading.AbandonedMutexException]{$ownsLaunchMutex=$true}
if(-not $ownsLaunchMutex){throw 'MinePeak ja esta iniciando nesta pasta. Aguarde a primeira abertura.'}
if(Get-Process -Name PEAK -ErrorAction SilentlyContinue){throw 'PEAK ja esta aberto. Feche o jogo antes de iniciar esta integracao.'}
$record=Join-Path $root 'local-config.json'
if(-not(Test-Path -LiteralPath $record)){throw 'A instalacao ainda nao foi preparada. Siga README-PT.md e execute Preparar.ps1.'}
try{$c=Get-Content -LiteralPath $record -Raw | ConvertFrom-Json}
catch{throw 'Nao foi possivel ler local-config.json. Confira o arquivo ou prepare a instalacao novamente.'}
if(-not $c.PSObject.Properties['flavor'] -or $c.flavor -notin @('coop','classic')){throw 'O perfil local nao identifica uma instalacao valida. Execute Preparar.ps1 novamente.'}
foreach($field in @('profile','steam','peak','java','gradle')){
    if(-not $c.PSObject.Properties[$field] -or [string]::IsNullOrWhiteSpace([string]$c.$field)){
        throw ('O perfil local esta incompleto ('+$field+'). Execute Preparar.ps1 novamente.')
    }
}
$target=Join-Path $c.profile 'BepInEx\core\BepInEx.Preloader.dll'
$required=@(
    @{Path=$c.steam;Name='Steam'},
    @{Path=(Join-Path $c.peak 'PEAK.exe');Name='PEAK'},
    @{Path=(Join-Path $c.java 'bin\java.exe');Name='Java'},
    @{Path=$target;Name='perfil BepInEx'},
    @{Path=(Join-Path $root 'fabric\gradle\wrapper\gradle-wrapper.jar');Name='inicializador Minecraft'}
)
foreach($item in $required){
    if(-not $item.Path -or -not(Test-Path -LiteralPath $item.Path -PathType Leaf)){
        throw ('Arquivo necessario nao encontrado: '+$item.Name+'. Prepare a instalacao novamente antes de jogar.')
    }
}
# A second Java can lock the same world and race the local bridge while the first
# one is still saving after PEAK closes. Never kill it or start another copy.
function Get-ProjectMinecraft {
    @(Get-CimInstance Win32_Process -Filter "Name='java.exe' OR Name='javaw.exe' OR Name='powershell.exe' OR Name='pwsh.exe'" | Where-Object {
        $_.CommandLine -and $_.CommandLine.IndexOf($root,[StringComparison]::OrdinalIgnoreCase) -ge 0 -and
        ($_.CommandLine -match 'fabric\.dli\.config=' -or $_.CommandLine -match '\brunClient\b' -or $_.CommandLine -match 'Iniciar-Minecraft\.ps1')
    })
}
$remaining=@(Get-ProjectMinecraft)
if($remaining.Count){
    Write-Host 'Aguardando o Minecraft anterior salvar e encerrar...'
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while($remaining.Count -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 350; $remaining=@(Get-ProjectMinecraft)}
    if($remaining.Count){throw 'O Minecraft desta pasta ainda esta aberto. Aguarde o salvamento terminar e tente novamente. Nenhuma segunda copia foi iniciada.'}
}
$env:JAVA_HOME=$c.java
$env:GRADLE_USER_HOME=$c.gradle
$env:TEMP=Join-Path $c.profile 'cache\tmp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
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
Start-Process -FilePath $c.steam -ArgumentList "-applaunch 3527290 --doorstop-enabled true --doorstop-target-assembly `"$target`" -force-d3d11 -screen-fullscreen 0 -screen-width 1280 -screen-height 720" -WindowStyle Hidden
Write-Host 'PEAK e Minecraft iniciando. Esc abre o botao Modo Minecraft; L alterna.'
}finally{
    if($ownsLaunchMutex){$launchMutex.ReleaseMutex()}
    $launchMutex.Dispose()
}
