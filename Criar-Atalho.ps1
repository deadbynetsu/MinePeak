$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$record=Join-Path $root 'local-config.json'
if(-not(Test-Path -LiteralPath $record)){throw 'Prepare a instalacao primeiro, conforme README-PT.md.'}
$config=Get-Content -LiteralPath $record -Raw | ConvertFrom-Json
$desktop=[Environment]::GetFolderPath('Desktop')
if(-not(Test-Path -LiteralPath $desktop)){throw 'Area de trabalho nao encontrada.'}
$file=Join-Path $desktop 'PEAK com Minecraft.lnk'
$shell=New-Object -ComObject WScript.Shell
if(Test-Path -LiteralPath $file){
    $existing=$shell.CreateShortcut($file)
    if($existing.Arguments -notlike ('*'+$root+'*')){$file=Join-Path $desktop ('PEAK com Minecraft - '+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.lnk')}
}
$shortcut=$shell.CreateShortcut($file)
$shortcut.TargetPath=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$shortcut.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $root 'Iniciar-PEAKCraft.ps1')+'"'
$shortcut.WorkingDirectory=$root
$shortcut.Description='PEAK da Steam com modo Minecraft opcional (Esc ou F8)'
$shortcut.IconLocation=(Join-Path $config.peak 'PEAK.exe')+',0'
$shortcut.WindowStyle=7
$shortcut.Save()
Write-Host "Atalho criado: $file"
