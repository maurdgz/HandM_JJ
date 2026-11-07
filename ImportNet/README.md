# ImportNet

Base real en ASP.NET Core **.NET 10**, PostgreSQL y Clean Architecture. Un solo host sirve API JSON y MVC con cookies de Identity. Repositorio: https://github.com/maurdgz/HandM_JJ.git.

La solución permanece en `C:\Users\Usuario\RiderProjects\ImportNet\ImportNet.sln`. Los comandos de esta guía se ejecutan desde su subcarpeta `ImportNet` (la carpeta de código original de Rider).

```text
ImportNet.sln
ImportNet/
  ImportNet.API/              Host, Controllers/Api, Controllers/Web, Views
  ImportNet.Application/      UseCases/Categorias y Productos, MediatR
  ImportNet.Domain/           Entidades e interfaces de persistencia
  ImportNet.Infrastructure/   EF, Identity, repositorios y migraciones
  ImportNet.Tests/            Pruebas de aplicación e integración HTTP
  docs/                      Decisiones, estado y prueba manual
```

## Preparación

Requiere SDK .NET 10, PostgreSQL y la base **ImportNet ya existente**. No se crea ni se elimina la base automáticamente. No hay contraseñas en appsettings. Configura la conexión fuera del repositorio, sustituyendo el marcador solo en tu terminal local:

```powershell
dotnet user-secrets set "ConnectionStrings:ImportNet" "Host=localhost;Port=5432;Database=ImportNet;Username=postgres;Password=TU_CLAVE_LOCAL" --project ImportNet.API
dotnet tool restore
dotnet restore ../ImportNet.sln
dotnet build ../ImportNet.sln --no-restore
dotnet test ../ImportNet.sln --no-build
```

También se admite `ConnectionStrings__ImportNet` como variable de entorno. La factoría de diseño y el host leen el mismo identificador de secretos; las variables de entorno tienen prioridad. No ejecutes `user-secrets list` en logs compartidos.

## Migración revisable

`InitialCreate` y su snapshot están en `ImportNet.Infrastructure/Persistence/Migrations`. `docs/InitialCreate.sql` es el SQL de creación idempotente, sin datos ni credenciales. Antes de aplicar, comprueba con pgAdmin que la base correcta no contiene tablas de negocio preexistentes y revisa el SQL. La migración inicial crea tablas; su método Down, generado por EF, elimina las tablas creadas y **no debe ejecutarse sobre datos reales**.

```powershell
# Inspeccionar/generar SQL: no modifica la base
dotnet ef migrations script 0 InitialCreate --idempotent --project ImportNet.Infrastructure --startup-project ImportNet.API --output docs/InitialCreate.sql
# Solo después de comprobar el esquema existente:
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API
# Para cambios futuros del modelo:
dotnet ef migrations add NombreDelCambio --project ImportNet.Infrastructure --startup-project ImportNet.API --output-dir Persistence/Migrations
```

No se ha aplicado esta migración a PostgreSQL en esta ejecución: faltan secretos de conexión. Las pruebas usan una base SQLite transitoria independiente; no demuestran funcionamiento ni concurrencia en PostgreSQL.

## Alta explícita del administrador

Después de aplicar la migración, configura por user-secrets `Bootstrap:Email`, `Bootstrap:Nombre` y `Bootstrap:Password`. La contraseña debe tener al menos 12 caracteres, mayúscula, minúscula, número y símbolo. Elige una nueva contraseña local y nunca la guardes en archivos del proyecto.

```powershell
dotnet user-secrets set "Bootstrap:Email" "TU_CORREO_ADMIN" --project ImportNet.API
dotnet user-secrets set "Bootstrap:Nombre" "TU_NOMBRE_ADMIN" --project ImportNet.API
dotnet user-secrets set "Bootstrap:Password" "TU_CLAVE_LOCAL" --project ImportNet.API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project ImportNet.API -- --bootstrap-admin
dotnet user-secrets remove "Bootstrap:Password" --project ImportNet.API
dotnet run --project ImportNet.API
```

El bootstrap solo funciona en desarrollo, exige migraciones al día, crea los tres roles y asocia un Empleado al administrador dentro de una transacción. No cambia ni promueve cuentas existentes. No hay registro público ni contraseñas predeterminadas. CRUD administrativo de empleados pendiente.

Abre http://localhost:5080/cuenta/login. Swagger: http://localhost:5080/swagger (solo Development). Bootstrap 5.3.8 se sirve localmente, sin depender de CDN en ejecución; licencia incluida.

## API real

| Método | Ruta | Política |
| --- | --- | --- |
| GET | `/api/categorias`, `/api/categorias/{id}` | Administrador, Almacenero, Vendedor |
| POST | `/api/categorias` | Administrador, Almacenero |
| PUT, DELETE | `/api/categorias/{id}` | Administrador, Almacenero |
| GET | `/api/productos`, `/api/productos/{id}` | Administrador, Almacenero, Vendedor |
| POST | `/api/productos` | Administrador, Almacenero |
| PUT, DELETE | `/api/productos/{id}` | Administrador, Almacenero |

Crear responde 201 con Location e id; editar/eliminar, 204. Validación 400; sin sesión 401; rol insuficiente 403; inexistente 404; duplicados/referencias 409. Errores de negocio usan ProblemDetails, sin detalles internos.

Ejemplo de cuerpo para crear producto (primero crea una categoría):

```json
{
  "codigo": "USB-001",
  "nombre": "Cable USB",
  "descripcion": null,
  "unidadMedida": "Unidad",
  "peso": 0.125,
  "precioUsd": 4.50,
  "partidaArancelaria": "854442",
  "categoriaId": 1,
  "stockMinimo": 2
}
```

Para probar mutaciones API, inicia sesión en MVC y obtiene `GET /cuenta/csrf` en esa misma sesión. Envía el token devuelto como `X-CSRF-TOKEN` y conserva las cookies. Swagger no añade ese token automáticamente. Nunca uses IgnoreAntiforgeryToken para sortear esta protección.

Las vistas están en `/`, `/categorias`, `/productos`, `/categorias/crear`, `/productos/crear` y `/{modulo}/{id}/editar`. Los formularios guardan por POST `/{modulo}/guardar` y eliminan por POST `/{modulo}/{id}/eliminar`. Login y logout usan POST `/cuenta/login` y `/cuenta/logout`, con antiforgery.

## Alcance

Catálogo, login y MVC inicial funcionan en las pruebas automatizadas. Inventario y ventas tienen modelo relacional inicial, pero no tienen endpoints ni transacciones de stock implementadas. No se ofrecen ventas sin protección de stock. Reportes, exportaciones y administración de empleados quedan pendientes. Consulta `docs/ESTADO.md`, `docs/DECISIONES.md` y `docs/PRUEBA-MANUAL.md`.

El inventario completo de archivos está en `docs/ARCHIVOS.md` dentro de la carpeta de código `ImportNet`.
