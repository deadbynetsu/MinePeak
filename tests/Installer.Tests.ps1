param([string]$WorkDirectory = [IO.Path]::GetTempPath())
$ErrorActionPreference = 'Stop'
$source = Join-Path (Split-Path $PSScriptRoot -Parent) 'Instalar-Local.ps1'
$caseRoot = Join-Path $WorkDirectory ('MinePeak-installer-' + [guid]::NewGuid().ToString('N'))
$project = Join-Path $caseRoot 'project'
$game = Join-Path $caseRoot 'game'
$loader = Join-Path $caseRoot 'loader'
$localBuild = Join-Path $project 'coop/bin/Release/netstandard2.1/PeakCraftCoop.dll'
$releaseDll = Join-Path $project 'binarios/PeakCraftCoop.dll'
$installedDll = Join-Path $project 'local-profile/BepInEx/plugins/PeakCraft/PeakCraftCoop.dll'
foreach ($folder in @($project,$game,(Join-Path $project 'binarios'),(Join-Path $project 'peak'),
    (Join-Path $project 'referencia-coop'),(Split-Path $localBuild -Parent),(Join-Path $loader 'BepInEx/core'))) {
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
}
Copy-Item -LiteralPath $source -Destination (Join-Path $project 'Instalar-Local.ps1')
'fixture, never executed' | Set-Content (Join-Path $game 'PEAK.exe')
'loader fixture' | Set-Content (Join-Path $loader 'winhttp.dll')
'preloader fixture' | Set-Content (Join-Path $loader 'BepInEx/core/BepInEx.Preloader.dll')
'release coop' | Set-Content $releaseDll
'stale local compiler output' | Set-Content $localBuild
foreach ($file in @('peakcraftcoop-fabric.jar','aeironnarmiento.PeakCraft.dll','skycraft-0.1.2.jar')) {
    $file | Set-Content (Join-Path $project ('binarios/' + $file))
}
foreach ($file in @('peak/LICENSE','referencia-coop/LICENSE-PeakCraftCoop.txt','referencia-coop/THIRD-PARTY-NOTICES.txt')) {
    'MIT fixture' | Set-Content (Join-Path $project $file)
}
# This installer fixture starts no executable and never writes to a real Steam directory.
function Get-Process { param($Name,$ErrorAction) return $null }
function Get-ItemProperty { param($Path) return [pscustomobject]@{SteamPath=(Join-Path $caseRoot 'steam')} }
function Install-Fixture {
    & (Join-Path $project 'Instalar-Local.ps1') -PeakDir $game -LoaderDir $loader `
        -JavaHome (Join-Path $caseRoot 'java') -GradleHome (Join-Path $caseRoot 'gradle') @args | Out-Null
}
function Check($Condition,[string]$Name) {
    if (-not $Condition) { throw ('FAIL: ' + $Name) }
    Write-Output ('PASS: ' + $Name)
}
Install-Fixture
Check ((Get-Content $installedDll -Raw).Trim() -eq 'release coop') 'packaged DLL wins over stale local compiler output'
Install-Fixture -UsarBuildLocal
Check ((Get-Content $installedDll -Raw).Trim() -eq 'stale local compiler output') 'local compiler output requires an explicit developer switch'
Install-Fixture
Check ((Get-Content $installedDll -Raw).Trim() -eq 'release coop') 'normal reinstall restores the packaged DLL'
$backups = @(Get-ChildItem (Join-Path $project 'local-backup') -Recurse -File -Filter '*.bak')
$count = $backups.Count
Install-Fixture
Check (@(Get-ChildItem (Join-Path $project 'local-backup') -Recurse -File -Filter '*.bak').Count -eq $count) 'reinstall reuses existing verified original-file backups'
Install-Fixture -Legacy
Check ((Test-Path ($installedDll+'.disabled')) -and -not (Test-Path $installedDll)) 'legacy installation disables the Co-op DLL'
Install-Fixture
$legacyDll=Join-Path (Split-Path $installedDll -Parent) 'aeironnarmiento.PeakCraft.dll'
Check ((Test-Path ($legacyDll+'.disabled')) -and -not (Test-Path $legacyDll)) 'Co-op installation disables the legacy DLL'
Remove-Item -LiteralPath $releaseDll
$rejected=$false
try { Install-Fixture } catch { $rejected=$_.Exception.Message -match 'modo coop incompletos' }
Check $rejected 'missing packaged DLL never silently falls back to stale local output'
'release coop' | Set-Content $releaseDll
'unrelated loader' | Set-Content (Join-Path $game 'winhttp.dll')
$before=(Get-FileHash (Join-Path $game 'winhttp.dll')).Hash
$rejected=$false
try { Install-Fixture } catch { $rejected=$_.Exception.Message -match 'outro winhttp.dll' }
Check ($rejected -and (Get-FileHash (Join-Path $game 'winhttp.dll')).Hash -eq $before) 'unrelated game loader is refused and preserved'
Copy-Item (Join-Path $loader 'winhttp.dll') (Join-Path $game 'winhttp.dll') -Force
"[General]`nenabled=true`ntarget_assembly=another-mod.dll" | Set-Content (Join-Path $game 'doorstop_config.ini')
$before=(Get-FileHash (Join-Path $game 'doorstop_config.ini')).Hash
$rejected=$false
try { Install-Fixture } catch { $rejected=$_.Exception.Message -match 'Doorstop ativa ou desconhecida' }
Check ($rejected -and (Get-FileHash (Join-Path $game 'doorstop_config.ini')).Hash -eq $before) 'active Doorstop setup is refused and preserved'
Write-Output 'PASS: 9 installer regression cases; only fixture files were changed'
exit 0
