# ImportNet

Backend **ASP.NET Core Web API, .NET 10**, PostgreSQL y arquitectura hexagonal. Repositorio: https://github.com/maurdgz/HandM_JJ.git, rama `main`. El frontend futuro será Astro + TypeScript + Tailwind + shadcn/ui; no se desarrolla en esta jornada.

La solución original es `ImportNet.sln`. Los comandos siguientes se ejecutan desde la subcarpeta `ImportNet`.

## Ejecución local y Rider

Se conservan los User Secrets `ImportNet-local-2026`; nunca se incluyen credenciales en appsettings. Si falta la conexión, ejecutar `scripts/Configurar-Conexion.ps1` (contraseña oculta). No se crean ni reinician bases automáticamente.

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/Configurar-Jwt.ps1
dotnet restore ../ImportNet.sln
dotnet build ../ImportNet.sln --no-restore
dotnet run --project ImportNet.API --launch-profile ImportNet -- --inspect-database
# Revisar historial/esquema y SQL antes de aplicar cambios pendientes:
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API
dotnet run --project ImportNet.API --launch-profile ImportNet
```

Rider: usar el proyecto ImportNet.API y perfil `ImportNet`. Swagger: http://localhost:5080/swagger. OpenAPI: http://localhost:5080/swagger/v1/swagger.json, exclusivamente en Development. La raíz `/` responde 404: el host sirve una API REST, sin vistas MVC.

`Configurar-Jwt.ps1` genera una clave criptográfica aleatoria de 64 bytes en User Secrets si no existe. Conserva claves existentes; no muestra sus valores. Configuración requerida: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` (Base64, mínimo 32 bytes). En despliegues usar variables `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` y `ConnectionStrings__ImportNet`. No existe una clave de firma predeterminada. Cambiarla invalida todos los tokens.

## Autenticación y transición

1. Ejecutar `POST /api/auth/login` con JSON `{ "email": "correo configurado localmente", "password": "contraseña vigente" }`.
2. Copiar `accessToken` de la respuesta y pegarlo en **Authorize**, sin prefijo. Swagger envía `Authorization: Bearer ...`.
3. Ejecutar `GET /api/auth/me`; devuelve usuario, empleado y roles, sin hashes ni sellos de seguridad.

JWT HS256 dura dos horas y valida firma, emisor, audiencia y vencimiento sin tolerancia adicional. Cada petición contrasta empleado activo, roles y versión de acceso con PostgreSQL. Desactivar, modificar roles/perfil o cambiar la credencial invalida tokens anteriores. Cinco fallos de contraseña bloquean temporalmente la cuenta. El cliente termina su sesión descartando el token; no hay cookies, CSRF ni rutas `/cuenta/*`.

Las tablas AspNetUsers/AspNetRoles/AspNetUserRoles y sus relaciones se conservan. Identity Core permanece únicamente como adaptador de almacenamiento, validación y administración transaccional; no autentica HTTP. Usuario/Rol/UsuarioRol son representaciones de dominio proyectadas por el adaptador, sin duplicar cuentas ni identificadores.

Las cuentas anteriores usan su contraseña vigente: se verifica con el algoritmo Identity y, solo si es correcta, se guarda BCrypt. No se convierten hashes entre algoritmos ni se inventan contraseñas. Cuentas nuevas usan BCrypt desde el alta. Se utiliza BCrypt con costo 12 y prehash SHA-384 para evitar el límite de 72 bytes, identificado por `{bcrypt-sha384}`. No se publica el hash. El administrador local existente permanece intacto hasta su primer login correcto; no se solicitó ni se usó su contraseña para validar.

Si no existe ningún Administrador activo:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/Crear-Administrador.ps1
```

El script pide datos reales y contraseña oculta, ejecuta `--bootstrap-admin` y retira `Bootstrap:Password`. No hay claves conocidas. El bootstrap rechaza cuentas duplicadas y un segundo administrador inicial; posteriores cuentas se crean mediante `/api/empleados`. Se conserva la protección transaccional del último Administrador activo.

## Endpoints

| Método | Ruta | Permiso |
| --- | --- | --- |
| POST | `/api/auth/login` | Anónimo |
| GET | `/api/auth/me` | Usuario activo autenticado |
| GET | `/api/categorias`, `/api/categorias/{id}` | Administrador/Vendedor |
| POST | `/api/categorias` | Administrador |
| PUT, DELETE | `/api/categorias/{id}` | Administrador |
| GET | `/api/productos`, `/api/productos/{id}` | Administrador/Vendedor |
| POST | `/api/productos` | Administrador |
| PUT, DELETE | `/api/productos/{id}` | Administrador |
| GET, POST | `/api/empleados` | Administrador |
| GET, PUT | `/api/empleados/{id}` | Administrador |
| PATCH | `/api/empleados/{id}/estado` | Administrador |
| PUT | `/api/empleados/{id}/roles` | Administrador |
| GET | `/api/empleados/roles` | Administrador |

Crear responde 201 con Location; modificar/eliminar 204; validación 400, sin token válido 401, sin permisos 403, inexistente 404 y conflicto 409. Errores de aplicación usan ProblemDetails sin detalles internos. No se borra físicamente ningún empleado.

Producto: `codigo` es el SKU único; `precioVenta` y `costoPromedio` están expresados en PEN. `costoImportacionUsd` y `tipoCambioImportacion` son referencias opcionales proporcionadas conjuntamente; tipo de cambio en PEN por USD. No convierten precios ni calculan stock/promedios. PUT sustituye ambos valores; omitirlos los deja vacíos. Costo promedio, costo USD y tipo de cambio solo se devuelven al Administrador.

```json
{
  "codigo": "USB-001", "nombre": "Cable USB", "unidadMedida": "Unidad",
  "precioVenta": 12.50, "categoriaId": 1, "stockMinimo": 2,
  "costoImportacionUsd": 2.50, "tipoCambioImportacion": 3.85
}
```

## Validación

```powershell
dotnet restore ../ImportNet.sln
dotnet build ../ImportNet.sln --no-restore --configuration Release
dotnet test ../ImportNet.sln --no-build --configuration Release
dotnet list ../ImportNet.sln package --vulnerable --include-transitive
# PostgreSQL real, datos temporales dentro de transacciones con rollback:
$env:IMPORTNET_POSTGRES_TESTS = '1'
dotnet test ../ImportNet.sln --filter Database=PostgreSQL
Remove-Item Env:IMPORTNET_POSTGRES_TESTS
```

La suite ordinaria usa SQLite efímero; PostgreSQL se habilita explícitamente y exige migraciones al día. No crea/elimina bases ni aplica migraciones. Las secuencias pueden avanzar incluso con rollback. Chrome es opcional: necesita Python/Playwright y Chrome local, con `IMPORTNET_BROWSER_TESTS=1`. Se pueden mantener esas herramientas en una carpeta temporal y añadirla a PYTHONPATH. La prueba usa botones reales de Swagger, sin trazas ni capturas con credenciales.

GitHub Actions ejecuta restore/build/test en .NET 10. Inventario, compras, recepción, ventas transaccionales y reportes no tienen nuevos endpoints. Informe: `docs/INFORME-DIAS-02-03.md`; archivos: `docs/ARCHIVOS.md`. Los informes de los días 01 y de validación inicial son evidencia histórica y no instrucciones vigentes de autenticación.
