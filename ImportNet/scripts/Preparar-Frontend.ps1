param([switch]$SkipInstall)
$ErrorActionPreference = 'Stop'

$importNetRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$frontendRoot = Join-Path $importNetRoot 'frontend'
$apiRoot = Join-Path $importNetRoot 'ImportNet.API'
$targetRoot = [IO.Path]::GetFullPath((Join-Path $apiRoot 'wwwroot'))
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $importNetRoot 'ImportNet.API/wwwroot'))
$npmCommand = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }

Push-Location $frontendRoot
try {
    if (-not $SkipInstall) {
        & $npmCommand ci
        if ($LASTEXITCODE -ne 0) { throw 'No se pudieron instalar las dependencias del frontend.' }
    }
    & $npmCommand run check
    if ($LASTEXITCODE -ne 0) { throw 'El frontend tiene errores de tipos.' }
    & $npmCommand run build
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el frontend.' }
} finally { Pop-Location }

$distRoot = Join-Path $frontendRoot 'dist'
if (-not (Test-Path -LiteralPath (Join-Path $distRoot 'index.html'))) {
    throw 'No se encontro dist/index.html. No se modifica wwwroot.'
}
# Solo se reemplaza el directorio generado por este script, dentro de la API.
if ($targetRoot -ne $expectedRoot -or [IO.Path]::GetFileName($targetRoot) -ne 'wwwroot') {
    throw 'Ruta de destino inesperada.'
}
$marker = Join-Path $targetRoot '.importnet-generated'
if (Test-Path -LiteralPath $targetRoot) {
    $targetItem = Get-Item -LiteralPath $targetRoot
    if (($targetItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'wwwroot no puede ser un enlace.'
    }
    if (-not (Test-Path -LiteralPath $marker)) {
        throw 'wwwroot no fue generado por este script. Revise sus archivos antes de reemplazarlo.'
    }
    $links = Get-ChildItem -LiteralPath $targetRoot -Force -Recurse |
        Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 }
    if ($links) { throw 'wwwroot contiene enlaces. No se reemplaza.' }
    Remove-Item -LiteralPath $targetRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $targetRoot | Out-Null
Set-Content -LiteralPath $marker -Value 'Generado por Preparar-Frontend.ps1; no editar.'
Get-ChildItem -LiteralPath $distRoot -Force | Copy-Item -Destination $targetRoot -Recurse -Force
Write-Host 'Frontend preparado en ImportNet.API/wwwroot. Inicie la API para usar un solo host.'
