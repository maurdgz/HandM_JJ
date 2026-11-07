$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '../ImportNet.API/ImportNet.API.csproj'
$values = @{
    'Bootstrap:Nombre' = (Read-Host 'Nombres del administrador')
    'Bootstrap:Apellidos' = (Read-Host 'Apellidos')
    'Bootstrap:DocumentoIdentidad' = (Read-Host 'Documento de identidad')
    'Bootstrap:Email' = (Read-Host 'Correo de acceso')
    'Bootstrap:Telefono' = (Read-Host 'Telefono')
}
$securePassword = Read-Host 'Nueva clave (12 caracteres minimo, mayuscula, minuscula, numero y simbolo)' -AsSecureString
$previousOutputEncoding = $OutputEncoding
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
try {
    $credential = [System.Management.Automation.PSCredential]::new('bootstrap', $securePassword)
    $values['Bootstrap:Password'] = $credential.GetNetworkCredential().Password
    $values | ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron configurar los secretos.' }
    dotnet run --project $apiProject --launch-profile ImportNet -- --bootstrap-admin
    if ($LASTEXITCODE -ne 0) { throw 'No se creo el administrador. Revise los datos y la politica de clave.' }
} finally {
    dotnet user-secrets remove 'Bootstrap:Password' --project $apiProject
    $values.Clear()
    $credential = $null
    $securePassword.Dispose()
    $OutputEncoding = $previousOutputEncoding
}
