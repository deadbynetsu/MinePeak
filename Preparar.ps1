param([switch]$AceitarEula,[string]$PeakDir)
$ErrorActionPreference='Stop'
if(-not $AceitarEula){throw 'Leia https://www.minecraft.net/eula. Se concordar, execute Preparar.ps1 -AceitarEula.'}
$root=$PSScriptRoot
$cache=Join-Path $root 'runtime'
New-Item -ItemType Directory -Force $cache | Out-Null
function DownloadChecked($Url,$File,$Sha){
    if(-not(Test-Path $File)){Invoke-WebRequest -UseBasicParsing $Url -OutFile $File}
    if((Get-FileHash $File -Algorithm SHA256).Hash -ne $Sha){throw "Checksum incorreto: $File"}
}
$jdk=Join-Path $cache 'java25.zip'
DownloadChecked 'https://cdn.azul.com/zulu/bin/zulu25.38.107-ca-jdk25.0.5-win_x64.zip' $jdk '636341B426CBEB02B4A30F5FE073A44D0EF64A2F91D077855E8D20F807471BF8'
$java=Join-Path $cache 'zulu25.38.107-ca-jdk25.0.5-win_x64'
if(-not(Test-Path (Join-Path $java 'bin\java.exe'))){Expand-Archive $jdk -DestinationPath $cache -Force}
$loaderZip=Join-Path $cache 'BepInExPack_PEAK-5.4.75301.zip'
DownloadChecked 'https://thunderstore.io/package/download/BepInEx/BepInExPack_PEAK/5.4.75301/' $loaderZip 'A71432613AB0B2F560046BC341D949C9D853B4F5D5223AB5E7F2EF1F113522E7'
$loader=Join-Path $cache 'loader'
if(-not(Test-Path (Join-Path $loader 'BepInExPack_PEAK\winhttp.dll'))){Expand-Archive $loaderZip -DestinationPath $loader -Force}
& (Join-Path $root 'Instalar-Local.ps1') -PeakDir $PeakDir -LoaderDir (Join-Path $loader 'BepInExPack_PEAK') -JavaHome $java -GradleHome (Join-Path $cache 'gradle')
Write-Host 'Preparacao concluida. Abra JOGAR.cmd. Na primeira vez o Minecraft baixa bibliotecas e assets oficiais.'
