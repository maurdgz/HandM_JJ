# ImportNet

Sistema para una tienda importadora: empleados y roles, catálogo, compras, inventario, POS, historial y anulación de ventas con autenticación JWT.

**Stack:** ASP.NET Core Web API .NET 10, PostgreSQL (EF Core + Npgsql), MediatR, arquitectura hexagonal (Domain, Application, Infrastructure, API) y xUnit. Frontend: Astro + TypeScript + Tailwind con islas React y shadcn/ui.

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

# 4. Compilar el frontend para servirlo desde la API (requiere Node 24+)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./scripts/Preparar-Frontend.ps1

# 5. Ejecutar API y frontend en el mismo puerto
dotnet run --project ImportNet.API --launch-profile ImportNet
```

En despliegues usar las variables `ConnectionStrings__ImportNet`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__SigningKey` (Base64, mínimo 32 bytes).

**Aplicación:** http://localhost:5080/login. Repetir la preparación al cambiar el frontend; `-SkipInstall` omite npm ci si las dependencias ya están instaladas. No se necesita iniciar Astro para este modo.

**Swagger:** http://localhost:5080/swagger (solo en Development). Hacer login en `POST /api/auth/login`, pegar `accessToken` en **Authorize** sin prefijo.

## Probar

```powershell
dotnet build ../ImportNet.sln --configuration Release
dotnet test ../ImportNet.sln --no-build --configuration Release
# Opcional, contra PostgreSQL real (transacciones con rollback):
$env:IMPORTNET_POSTGRES_TESTS = '1'; dotnet test ../ImportNet.sln --filter Database=PostgreSQL
```

La suite normal usa SQLite en memoria. La prueba en Chrome es opcional (`IMPORTNET_BROWSER_TESTS=1`, requiere Python y Playwright).

## Proveedores y clientes (issue #5)

Las páginas `/proveedores` y `/clientes` incluyen buscador. El administrador gestiona proveedores y su relación con productos; el vendedor puede registrar y consultar clientes, pero no administrar proveedores ni editar o eliminar clientes.

- Los casos de uso de terceros consumen los puertos `IProveedorRepository` e `IClienteRepository` mediante `IUnitOfWork`. El adaptador EF comparte la unidad de trabajo; `ITercerosRepository` se conserva como contrato compuesto para compatibilidad con compras.
- `GET /api/clientes/por-documento/{documento}` busca por documento completo, conserva ceros iniciales y requiere autenticación. Devuelve `200` si existe, `404` si no existe y `400` para formatos inválidos.
- Al registrar o editar se exige DNI de 8 o RUC de 11 dígitos ASCII. Se valida el formato, sin consulta externa a RENIEC/SUNAT. Los documentos duplicados devuelven `409`.
- El seed `Cliente varios` (`00000000`) puede consultarse, pero no editarse ni eliminarse.

Verificación específica: `dotnet test ../ImportNet.sln --configuration Release --filter FullyQualifiedName~TercerosTests` desde `ImportNet`. Cubre documentos, duplicados, seed, sustitución de productos del proveedor y autorización HTTP por rol.

## Documentación

- [Estado y desviaciones del plan](ImportNet/docs/ESTADO.md)
- [Decisiones vigentes](ImportNet/docs/DECISIONES.md)
- [Prueba manual y endpoints](ImportNet/docs/PRUEBA-MANUAL.md)
- [Día 07: compras, costos y contrato para recepción](ImportNet/docs/DIA-07-COMPRAS.md)
- [Día 08: recepción e inventario](ImportNet/docs/DIA-08-RECEPCION-INVENTARIO.md)
- [Día 09: ajustes y kardex](ImportNet/docs/DIA-09-AJUSTES-KARDEX.md)
- [Historial, nota y anulación](ImportNet/docs/VENTAS-HISTORIAL-ANULACION.md)
- [Día 16: seguridad y frontend servido por la API](ImportNet/docs/DIA-16-SEGURIDAD.md)
