# ImportNet

API REST para la gestión de una tienda importadora: catálogo, empleados y roles, autenticación JWT.

**Stack:** ASP.NET Core Web API .NET 10, PostgreSQL (EF Core + Npgsql), MediatR, arquitectura hexagonal (Domain, Application, Infrastructure, API) y xUnit. Frontend previsto: Astro + TypeScript + Tailwind con islas React (shadcn/ui, Recharts).

## Levantar en local

Los comandos se ejecutan desde la carpeta `ImportNet`. Las credenciales viven en User Secrets (`ImportNet-local-2026`), nunca en `appsettings`.

```powershell
# 1. Secretos: conexión PostgreSQL y clave JWT (Configurar-Jwt conserva una clave existente)
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Conexion.ps1
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Jwt.ps1

# 2. Migraciones: revisar el estado y el SQL antes de aplicar
dotnet run --project ImportNet.API --launch-profile ImportNet -- --inspect-database
dotnet ef migrations script --idempotent --project ImportNet.Infrastructure --startup-project ImportNet.API
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API

# 3. Primer administrador (solo si no hay ninguno activo)
powershell -ExecutionPolicy Bypass -File ./scripts/Crear-Administrador.ps1

# 4. Ejecutar
dotnet run --project ImportNet.API --launch-profile ImportNet
```

En despliegues usar las variables `ConnectionStrings__ImportNet`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__SigningKey` (Base64, mínimo 32 bytes).

**Swagger:** http://localhost:5080/swagger (solo en Development). Hacer login en `POST /api/auth/login`, pegar `accessToken` en **Authorize** sin prefijo.

## Probar

```powershell
dotnet build ../ImportNet.sln --configuration Release
dotnet test ../ImportNet.sln --no-build --configuration Release
# Opcional, contra PostgreSQL real (transacciones con rollback):
$env:IMPORTNET_POSTGRES_TESTS = '1'; dotnet test ../ImportNet.sln --filter Database=PostgreSQL
```

La suite normal usa SQLite en memoria. La prueba en Chrome es opcional (`IMPORTNET_BROWSER_TESTS=1`, requiere Python y Playwright).

## Documentación

- [Estado y desviaciones del plan](ImportNet/docs/ESTADO.md)
- [Decisiones vigentes](ImportNet/docs/DECISIONES.md)
- [Prueba manual y endpoints](ImportNet/docs/PRUEBA-MANUAL.md)
