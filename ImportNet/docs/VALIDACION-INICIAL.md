# Validación de PostgreSQL y Swagger

> Registro histórico del 06/11/2026. La limitación de conexión que se describe aquí quedó resuelta el 07/11/2026: ver `DIA-01-EMPLEADOS.md` para migraciones aplicadas e integración real verificada.

## PostgreSQL: pendiente de secreto local

No se encontró `ConnectionStrings:ImportNet` en el almacén User Secrets `ImportNet-local-2026`, ni una conexión equivalente en el entorno. No se han inspeccionado las tablas ni el historial real de la base y no se ha aplicado InitialCreate. No se han eliminado datos ni ejecutado cambios sobre PostgreSQL.

El propietario debe ejecutar desde la carpeta de código `ImportNet`:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Conexion.ps1
dotnet run --project ImportNet.API/ImportNet.API.csproj --launch-profile ImportNet -- --inspect-database
```

El script pide la contraseña sin mostrarla, usa un constructor de cadena de conexión para escapar caracteres especiales y pasa JSON por entrada estándar a User Secrets. No incluye la contraseña en archivos del proyecto ni argumentos del proceso. El comando de inspección es exclusivamente de lectura: comprueba base ImportNet/usuario postgres y lista tablas y migraciones. No aplica cambios automáticamente.

Si la base no tiene tablas de negocio y no hay historial, se puede revisar `InitialCreate.sql` y aplicar la migración inicial. Si hay tablas ajenas o un historial incompatible, detener la aplicación de migraciones e investigar; no sobreescribir ni eliminar esas tablas.

```powershell
dotnet ef migrations script 0 InitialCreate --idempotent --project ImportNet.Infrastructure --startup-project ImportNet.API
# Solo después de revisar el resultado de la inspección:
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API
$env:IMPORTNET_POSTGRES_TESTS = "1"
dotnet test ../ImportNet.sln --filter Database=PostgreSQL
Remove-Item Env:IMPORTNET_POSTGRES_TESTS
```

Las pruebas PostgreSQL están implementadas pero **no ejecutadas** en ausencia de secretos. Son tres casos (Administrador, Almacenero, Vendedor), con HTTP real/Kestrel y Npgsql, sobre una transacción por caso que termina con rollback. Comprueban login/logout, permisos, CRUD y restricciones. No crean/eliminan bases, no cambian el esquema y no modifican filas existentes. Las secuencias pueden avanzar incluso con rollback. Esta evidencia no debe confundirse con SQLite.

## Swagger y frontend

- Comando de arranque comprobado: `dotnet run --project ImportNet.API/ImportNet.API.csproj --launch-profile ImportNet`.
- Swagger: http://localhost:5080/swagger (redirige a `/swagger/index.html`).
- OpenAPI: http://localhost:5080/swagger/v1/swagger.json; documento 3.0.4, cuatro rutas y diez operaciones, exclusivamente categorías/productos.
- Frontend MVC: http://localhost:5080/; login: http://localhost:5080/cuenta/login.
- Swagger UI y login devolvieron HTTP 200 desde el proceso real. Chrome headless comprobó renderizado y bloqueo de mutaciones sin sesión.
- Tras corregir el interceptor, pasó la prueba de botones reales de Swagger con SQLite transitorio y Kestrel: login, GET/POST/PUT/DELETE de categorías y productos, conflicto al eliminar categoría referenciada y logout. Son 15 comprobaciones de navegador. Esta prueba no es una verificación PostgreSQL.
- La descripción de Swagger permite iniciar sesión en MVC y regresar. El interceptor usa las cookies existentes y solicita un token nuevo a `/cuenta/csrf` por cada mutación. No persiste tokens ni añade autenticación alternativa; CSRF, roles y cookies HttpOnly siguen activos.
- Se corrigió la forma de registrar el interceptor: el analizador de Swashbuckle necesita una función con cuerpo, no una función flecha sin llaves. La prueba real detectó el encabezado CSRF ausente y el servidor rechazó correctamente el POST con 400 antes de la corrección.
- No se añadieron módulos de empleados, inventario, proveedores ni ventas. Las rutas web y REST conservan sus controladores separados.

## Reproducción de pruebas

Resultado final observado: restore correcto, build con **0 errores y 0 advertencias**, **23 pruebas .NET aprobadas y una suite PostgreSQL omitida** (se habilitó IMPORTNET_BROWSER_TESTS), y **6 pruebas JavaScript aprobadas**. Auditoría NuGet con dependencias transitivas: ningún paquete vulnerable reportado. EF confirmó que el modelo coincide con InitialCreate. Sin la opción de navegador, la suite ordinaria omite también esa prueba opcional.

```powershell
dotnet restore ../ImportNet.sln
dotnet build ../ImportNet.sln --no-restore
dotnet test ../ImportNet.sln --no-build
node --test ImportNet.Tests/swagger-auth.test.cjs
dotnet list ../ImportNet.sln package --vulnerable --include-transitive
```

La prueba opcional de Chrome requiere Python, Playwright y Chrome local. Se instaló Playwright exclusivamente en una carpeta temporal de herramientas, fuera del repositorio. Las contraseñas de usuarios de prueba son aleatorias y viajan por entrada estándar al navegador, sin capturas, trazas ni logs de sus valores. Los fallos solo muestran el paso, el código HTTP y presencia/ausencia de CSRF.

Para repetir la prueba de botones con SQLite: configurar `IMPORTNET_BROWSER_TESTS=1`, hacer disponible Playwright en el entorno Python y ejecutar `dotnet test ../ImportNet.sln --filter Browser=Chrome`. Con PostgreSQL habilitado, la misma prueba de navegador se ejecuta también dentro del caso Administrador y su transacción de prueba.
