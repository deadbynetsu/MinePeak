$ErrorActionPreference='Stop'
$c=Get-Content (Join-Path $PSScriptRoot 'local-config.json') -Raw | ConvertFrom-Json
$env:JAVA_HOME=$c.java
$env:GRADLE_USER_HOME=$c.gradle
$env:JAVA_TOOL_OPTIONS='-Dskycraft.startHidden=true'
# Read only the active profile's public identity and its own cached skin; never print account data.
$name='Jogador'
$uuid=$null
$skin=$null
$accounts=Join-Path $env:APPDATA '.sklauncher\accounts.json'
if(Test-Path $accounts){
    try{
        $data=Get-Content $accounts -Raw | ConvertFrom-Json
        $selected=@($data.accounts | Where-Object id -eq $data.activeAccountId)[0]
        if($selected.username -match '^[A-Za-z0-9_]{3,16}$'){$name=$selected.username}
        if($selected.uuid){
            $uuid=[guid]$selected.uuid
            $id=$uuid.ToString('N')
            $skins=Join-Path $env:APPDATA '.sklauncher\assets\skins'
            if(Test-Path $skins){$skin=Get-ChildItem $skins -Recurse -File -Filter "$id*" | Select-Object -First 1 -ExpandProperty FullName}
        }
    }catch{Write-Warning 'Perfil local indisponivel; usando nome de jogador padrao.'}
}
if($c.username -match '^[A-Za-z0-9_]{3,16}$'){$name=$c.username}
if($c.skinFile -and (Test-Path -LiteralPath $c.skinFile)){$skin=(Resolve-Path -LiteralPath $c.skinFile).Path}
$env:SKYCRAFT_USERNAME=$name
$env:SKYCRAFT_UUID=if($uuid){$uuid.ToString()}else{''}
$env:PEAKCRAFT_SKIN_FILE=$skin
$env:PEAKCRAFT_SKIN_MODEL=if($c.skinModel -eq 'slim'){'slim'}else{'wide'}
Write-Host "Perfil local: $name. Skin local encontrada: $([bool]$skin)"
if($c.flavor -eq 'coop'){
    $project=Join-Path $PSScriptRoot 'coop-fabric'
}else{$project=Join-Path $PSScriptRoot 'fabric'}
Push-Location $project
try{ & (Join-Path $PSScriptRoot 'fabric\gradlew.bat') runClient --no-configuration-cache --no-daemon }
finally{Pop-Location}
