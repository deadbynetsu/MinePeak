param([string]$PeakDir, [string]$LoaderDir, [string]$JavaHome, [string]$GradleHome, [switch]$Legacy, [switch]$UsarBuildLocal)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(Get-Process -Name PEAK -ErrorAction SilentlyContinue){throw 'Feche PEAK antes de instalar ou atualizar os arquivos.'}
$steam=(Get-ItemProperty 'HKCU:\Software\Valve\Steam').SteamPath
if(-not $PeakDir){$PeakDir=Join-Path $steam 'steamapps\common\PEAK'}
$PeakDir=[IO.Path]::GetFullPath($PeakDir).TrimEnd('\','/')
if(-not (Test-Path (Join-Path $PeakDir 'PEAK.exe'))){throw 'PEAK.exe nao encontrado. Informe -PeakDir.'}
if(-not $LoaderDir){$LoaderDir=Join-Path (Split-Path (Split-Path $root -Parent) -Parent) 'work\bepinex-peak\BepInExPack_PEAK'}
if(-not (Test-Path (Join-Path $LoaderDir 'winhttp.dll'))){throw 'Loader BepInExPack_PEAK nao encontrado.'}
if(-not (Test-Path (Join-Path $LoaderDir 'BepInEx\core\BepInEx.Preloader.dll'))){throw 'Loader incompleto: BepInEx.Preloader.dll nao encontrado.'}

# Only a binary of the same flavor can be used: each flavor has its own mapping name.
if($Legacy){
    $dll=Join-Path $root 'binarios\aeironnarmiento.PeakCraft.dll'
    $jar=Join-Path $root 'binarios\skycraft-0.1.2.jar'
    if($UsarBuildLocal){
        $dll=Join-Path $root 'peak\artifacts\bin\PeakCraft\release\aeironnarmiento.PeakCraft.dll'
        $jar=Join-Path $root 'fabric\build\libs\skycraft-0.1.2.jar'
    }
    $flavor='classic'
}else{
    # A stale compiler output must never silently replace a newer Release DLL.
    $dll=Join-Path $root 'binarios\PeakCraftCoop.dll'
    if($UsarBuildLocal){$dll=Join-Path $root 'coop\bin\Release\netstandard2.1\PeakCraftCoop.dll'}
    $jar=Join-Path $root 'binarios\peakcraftcoop-fabric.jar'
    $flavor='coop'
}
if(-not(Test-Path $dll) -or -not(Test-Path $jar)){throw "Binarios do modo $flavor incompletos. Instale o pacote completo ou compile as duas metades desse modo. Nenhum outro modo foi usado como substituto."}
$record=Join-Path $root 'local-config.json'
$previous=$null
if(Test-Path $record){$previous=Get-Content $record -Raw | ConvertFrom-Json}

# Snapshots are keyed by game directory and content hash; previous originals remain untouched.
$hashTool=[Security.Cryptography.SHA256]::Create()
try{$pathHash=[BitConverter]::ToString($hashTool.ComputeHash([Text.Encoding]::UTF8.GetBytes($PeakDir.ToLowerInvariant()))).Replace('-','').Substring(0,16)}
finally{$hashTool.Dispose()}
$backupDir=Join-Path $root ('local-backup\'+$pathHash)
New-Item -ItemType Directory -Force $backupDir | Out-Null
$manifestPath=Join-Path $backupDir 'backup-info.json'
$snapshots=@()
if(Test-Path $manifestPath){
    $manifest=Get-Content $manifestPath -Raw | ConvertFrom-Json
    if(-not [string]::Equals($manifest.peak,$PeakDir,[StringComparison]::OrdinalIgnoreCase)){throw 'Diretorio de backup pertence a outra instalacao. Nenhum arquivo do jogo foi alterado.'}
    $snapshots=@($manifest.files)
}
foreach($file in @('winhttp.dll','doorstop_config.ini')){
    $existing=Join-Path $PeakDir $file
    if(-not(Test-Path -LiteralPath $existing)){continue}
    $sha=(Get-FileHash -LiteralPath $existing -Algorithm SHA256).Hash
    $backupName=$file+'.'+$sha+'.bak'
    $backupFile=Join-Path $backupDir $backupName
    if(-not(Test-Path -LiteralPath $backupFile)){Copy-Item -LiteralPath $existing -Destination $backupFile}
    if((Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash -ne $sha){throw "Backup com checksum incorreto: $backupFile. Nenhum arquivo do jogo foi alterado."}
    if(-not @($snapshots | Where-Object { $_.name -eq $file -and $_.sha256 -eq $sha }).Count){
        $snapshots+=@{name=$file;sha256=$sha;backup=$backupName;capturedUtc=[DateTime]::UtcNow.ToString('o')}
    }
}
@{peak=$PeakDir;files=@($snapshots)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8

# Avoid replacing a different loader or disabling an existing active mod installation.
$currentLoader=Join-Path $PeakDir 'winhttp.dll'
if((Test-Path -LiteralPath $currentLoader) -and (Get-FileHash -LiteralPath $currentLoader -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath (Join-Path $LoaderDir 'winhttp.dll') -Algorithm SHA256).Hash){
    throw "Existe outro winhttp.dll nessa instalacao. Foi preservado em $backupDir; o jogo nao foi alterado. Use uma instalacao sem conflito de loader."
}
$doorstopFile=Join-Path $PeakDir 'doorstop_config.ini'
if(Test-Path -LiteralPath $doorstopFile){
    $currentConfig=Get-Content -LiteralPath $doorstopFile -Raw
    if($currentConfig -notmatch '(?im)^\s*enabled\s*=\s*false\s*$'){
        throw "Existe uma configuracao Doorstop ativa ou desconhecida. Foi preservada em $backupDir; o jogo nao foi alterado."
    }
}

$profile=Join-Path $root 'local-profile'
$plugins=Join-Path $profile 'BepInEx\plugins\PeakCraft'
New-Item -ItemType Directory -Force $profile,$plugins,(Join-Path $profile 'BepInEx\config') | Out-Null
Copy-Item (Join-Path $LoaderDir 'BepInEx\*') (Join-Path $profile 'BepInEx') -Recurse -Force
$otherName=if($Legacy){'PeakCraftCoop.dll'}else{'aeironnarmiento.PeakCraft.dll'}
$otherPlugin=Join-Path $plugins $otherName
if(Test-Path -LiteralPath $otherPlugin){Move-Item -LiteralPath $otherPlugin -Destination ($otherPlugin+'.disabled') -Force}
Copy-Item $dll,(Join-Path $root 'peak\LICENSE'),$jar $plugins -Force
if(-not $Legacy){Copy-Item (Join-Path $root 'referencia-coop\LICENSE-PeakCraftCoop.txt'),(Join-Path $root 'referencia-coop\THIRD-PARTY-NOTICES.txt') $plugins -Force}
Copy-Item -LiteralPath (Join-Path $LoaderDir 'winhttp.dll') -Destination $currentLoader -Force
# Steam's normal launch stays vanilla; our shortcut enables the external profile.
@'
[General]
enabled=false
target_assembly=BepInEx\core\BepInEx.Preloader.dll
redirect_output_log=false
[UnityMono]
debug_enabled=false
'@ | Set-Content -LiteralPath $doorstopFile -Encoding ascii
$workspace=Split-Path (Split-Path $root -Parent) -Parent
if(-not $JavaHome){$JavaHome=Join-Path $workspace 'work\jdk25\zulu25.38.107-ca-jdk25.0.5-win_x64'}
if(-not $GradleHome){$GradleHome=Join-Path $workspace 'work\gradle-peakcraft'}
$config=@{peak=$PeakDir;profile=$profile;java=$JavaHome;gradle=$GradleHome;steam=(Join-Path $steam 'steam.exe');flavor=$flavor}
if($previous){foreach($key in @('username','skinFile','skinModel')){if($previous.PSObject.Properties[$key] -and $previous.$key){$config[$key]=$previous.$key}}}
$config | ConvertTo-Json | Set-Content $record -Encoding utf8
if(-not $Legacy){
    $cfg=Join-Path $profile 'BepInEx\config\PeakCraftCoop.cfg'
    if(-not(Test-Path $cfg)){
        $mcJar=Join-Path $GradleHome 'caches\fabric-loom\26.3\minecraft-client.jar'
        @"
[Coop]
Mode = Viewer
ToggleModeKey = L
Share = true
MinecraftJar = $mcJar
[Dev]
Harness = false
CoopLoopback = false
"@ | Set-Content $cfg -Encoding utf8
    }
}
Write-Host "Instalado ($flavor). Backups verificados em $backupDir. Abertura normal pela Steam continua sem o plugin."
