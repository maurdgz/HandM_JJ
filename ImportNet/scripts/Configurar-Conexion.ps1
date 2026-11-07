$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '../ImportNet.API/ImportNet.API.csproj'
$securePassword = Read-Host 'Contraseña LOCAL de postgres (no se mostrará)' -AsSecureString
$previousOutputEncoding = $OutputEncoding
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
try {
    $credential = [System.Management.Automation.PSCredential]::new('postgres', $securePassword)
    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    $connection['Host'] = 'localhost'
    $connection['Port'] = 5432
    $connection['Database'] = 'ImportNet'
    $connection['Username'] = 'postgres'
    $connection['Password'] = $credential.GetNetworkCredential().Password
    @{ 'ConnectionStrings:ImportNet' = $connection.ConnectionString } |
        ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar User Secrets.' }
    Write-Host 'Conexión guardada en User Secrets fuera del repositorio.'
} finally {
    if ($null -ne $connection) { $connection.Clear() }
    $credential = $null
    $securePassword.Dispose()
    $OutputEncoding = $previousOutputEncoding
}
