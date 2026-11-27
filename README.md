# ImportNet

Sistema de gestión para una tienda importadora: empleados y roles, catálogo, proveedores/clientes, compras de importación con prorrateo de costos, inventario y kardex, punto de venta (POS), historial y anulación de ventas, reportes con exportación a Excel, dashboard por rol y **asistente de análisis de negocio con IA**.

## Stack y Arquitectura

- **Backend:** ASP.NET Core Web API (.NET 10), PostgreSQL (EF Core 10 + Npgsql), MediatR + Pipeline Behaviors (`LoggingBehavior`, `ValidationBehavior`), FluentValidation, ClosedXML, BCrypt + JWT HS256.
- **Arquitectura Hexagonal + DDD:**
  - `ImportNet.Domain`: entidades (`Entity`), reglas de negocio, excepciones de dominio (`DomainException`) y puertos (`IUnitOfWork`, repositorios).
  - `ImportNet.Application`: casos de uso CQRS con MediatR, validadores FluentValidation, behaviors transversales y puerto `IAsistenteIaService`.
  - `ImportNet.Infrastructure`: persistencia EF Core/PostgreSQL, `UnitOfWork` con transacciones `Serializable`, repositorios optimizados sin N+1, `ExportadorExcel` y adaptador externo `AsistenteIaService` (Gemini / OpenAI).
  - `ImportNet.API`: controladores `sealed`, middlewares (`RequestLoggingMiddleware`, `SecurityHeadersMiddleware`, `GlobalExceptionHandler`), Rate Limiting de login, `/health` y hosting del frontend compilado.
- **Frontend:** Astro + TypeScript + Tailwind CSS + React + shadcn/ui.

## Levantar en local

Los comandos se ejecutan desde `ImportNet`. Las credenciales locales viven en User Secrets (`ImportNet-local-2026`).

```powershell
# 1. Secretos locales: PostgreSQL y clave JWT
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Conexion.ps1
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Jwt.ps1

# 2. Migraciones
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API

# 3. Primer administrador
powershell -ExecutionPolicy Bypass -File ./scripts/Crear-Administrador.ps1

# 4. Compilar frontend Astro para servirlo desde la API (Node 24+)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./scripts/Preparar-Frontend.ps1

# 5. Ejecutar API + Frontend en http://localhost:5080
dotnet run --project ImportNet.API --launch-profile ImportNet
```

### Configurar el Asistente IA (Gemini u OpenAI)

En desarrollo local (User Secrets) o en producción (variables de entorno):

```bash
Ia__Provider=Gemini          # "Gemini" o "OpenAI"
Ia__Model=gemini-flash-latest # o "gpt-4o-mini" para OpenAI
Ia__ApiKey=<TU_API_KEY>
```

Endpoint: `POST /api/ia/analizar` (solo `Administrador`) e integrado visualmente en `/reportes`.

## Despliegue en Render + PostgreSQL en la nube

El repositorio incluye un `Dockerfile` multi-stage (compila Astro + .NET 10 API en un solo servicio) y el blueprint `render.yaml`:

1. En **Render Dashboard**, ir a **New + → Blueprint** y conectar este repositorio/rama.
2. Render creará automáticamente la base PostgreSQL (`importnet-db`) y el Web Service Docker (`importnet-web`) con `/health`.
3. Completar las 3 variables secretas solicitadas (`sync: false`):
   - `Jwt__SigningKey`: generar con `openssl rand -base64 32`
   - `Ia__ApiKey`: clave API de Google Gemini (o OpenAI si `Ia__Provider=OpenAI`)
   - `Bootstrap__Password`: contraseña inicial del administrador (mínimo 12 caracteres, mayúscula, minúscula, número y símbolo)
4. Al iniciar, `IMPORTNET_APPLY_MIGRATIONS=true` aplica las migraciones automáticamente sobre PostgreSQL en la nube y `Bootstrap__AutoCreateOnStartup=true` crea el administrador inicial de forma idempotente.

## Pruebas

```bash
dotnet restore ImportNet.sln
dotnet build ImportNet.sln --no-restore --configuration Release
dotnet test ImportNet.sln --no-build --configuration Release
```
