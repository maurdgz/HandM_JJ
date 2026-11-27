# ImportNet

Sistema para una tienda importadora: empleados y roles, catálogo, compras, inventario, POS, historial, anulación de ventas y dashboard por rol con autenticación JWT.

**Stack:** ASP.NET Core Web API .NET 10, PostgreSQL (EF Core + Npgsql), MediatR, arquitectura hexagonal (Domain, Application, Infrastructure, API) y xUnit. Frontend: Astro + TypeScript + Tailwind con islas React y shadcn/ui.

## Levantar en local



En despliegues usar las variables `ConnectionStrings__ImportNet`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__SigningKey` (Base64, mínimo 32 bytes).

**Aplicación:** http://localhost:5080/login. Repetir la preparación al cambiar el frontend; `-SkipInstall` omite npm ci si las dependencias ya están instaladas. No se necesita iniciar Astro para este modo.

**Swagger:** http://localhost:5080/swagger (solo en Development). Hacer login en `POST /api/auth/login`, pegar `accessToken` en **Authorize** sin prefijo.


La suite normal usa SQLite en memoria. La prueba en Chrome es opcional (`IMPORTNET_BROWSER_TESTS=1`, requiere Python y Playwright).

## Proveedores y clientes (issue #5)

Las páginas `/proveedores` y `/clientes` incluyen buscador. El administrador gestiona proveedores y su relación con productos; el vendedor puede registrar y consultar clientes, pero no administrar proveedores ni editar o eliminar clientes.

- Los casos de uso de terceros consumen los puertos `IProveedorRepository` e `IClienteRepository` mediante `IUnitOfWork`. El adaptador EF comparte la unidad de trabajo; `ITercerosRepository` se conserva como contrato compuesto para compatibilidad con compras.
- `GET /api/clientes/por-documento/{documento}` busca por documento completo, conserva ceros iniciales y requiere autenticación. Devuelve `200` si existe, `404` si no existe y `400` para formatos inválidos.
- Al registrar o editar se exige DNI de 8 o RUC de 11 dígitos ASCII. Se valida el formato, sin consulta externa a RENIEC/SUNAT. Los documentos duplicados devuelven `409`.
- El seed `Cliente varios` (`00000000`) puede consultarse, pero no editarse ni eliminarse.

Verificación específica: `dotnet test ../ImportNet.sln --configuration Release --filter FullyQualifiedName~TercerosTests` desde `ImportNet`. Cubre documentos, duplicados, seed, sustitución de productos del proveedor y autorización HTTP por rol.

