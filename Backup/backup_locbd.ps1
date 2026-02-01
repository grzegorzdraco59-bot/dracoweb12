# Pełny backup bazy locbd (MariaDB/MySQL)
# Uruchom: .\backup_locbd.ps1
# Wymaga: MariaDB 10.6 bin w PATH lub zmień $mysqldump

$ErrorActionPreference = "Stop"
$mysqldump = "C:\Program Files\MariaDB 10.6\bin\mysqldump.exe"
$outDir = $PSScriptRoot
$ts = Get-Date -Format "yyyyMMdd_HHmm"
$outFile = Join-Path $outDir "backup_locbd_$ts.sql"

Write-Host "Backup bazy locbd do: $outFile"
& $mysqldump --single-transaction --routines --triggers --events --add-drop-table `
  --databases locbd -h localhost -P 3306 -u root -p"dracogk0909" --result-file=$outFile

if ($LASTEXITCODE -ne 0) { throw "mysqldump zakończył się kodem: $LASTEXITCODE" }

$f = Get-Item $outFile
Write-Host "Sukces. Rozmiar: $([math]::Round($f.Length/1MB, 2)) MB"
