$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '../ImportNet.API/ImportNet.API.csproj'
# Preserve an existing signing key: running this procedure must not revoke sessions accidentally.
$rawSecrets = dotnet user-secrets list --project $apiProject --json 2>$null
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron leer los secretos locales.' }
$existing = ($rawSecrets -join "`n") | ConvertFrom-Json
if ($existing.'Jwt:SigningKey') { Write-Host 'JWT ya configurado. Se conserva la clave existente.'; exit 0 }
$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
$values = @{
    'Jwt:Issuer' = 'ImportNet'
    'Jwt:Audience' = 'ImportNet.Frontend'
    'Jwt:SigningKey' = [Convert]::ToBase64String($bytes)
}
try {
    $values | ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la configuración JWT.' }
    Write-Host 'Configuración JWT creada en User Secrets, sin mostrar la clave.'
} finally { [Array]::Clear($bytes, 0, $bytes.Length); $values.Clear(); $existing = $null; $rawSecrets = $null }
