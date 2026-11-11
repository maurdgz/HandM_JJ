# Informe Días 02 y 03 — 11/11/2026

## 1. Estado inicial de GitHub

Rama main y checkout limpio, ambos en 6a81905674ac1d026ca744f398e8c856454dd0a5, confirmado mediante fetch. Identidad existente: maurdgz, sin cambios de autor/fecha ni reescritura de historial. Se leyeron los informes y el código de los cinco proyectos .NET 10. PostgreSQL accesible mediante secretos locales: dos migraciones aplicadas, cuatro pendientes, un usuario y un empleado. Tablas de negocio afectadas vacías. No existía workflow.

## 2. Día 02 existente

Puertos ICategoriaRepository/IProductoRepository, GenericRepository, adaptadores LINQ ordenados con AsNoTracking, UnitOfWork con guardado atómico/traducción de conflictos y handlers sin EF. Administración de empleados ya contaba con contrato específico y transacción con protección del último administrador. Productos con precio/costo promedio PEN, SKU único y stock mínimo; clientes locales, almacén Principal y modelo de venta mostrador. Se conservaron dbc779f, 7aebed6 y 6a81905.

## 3. Recuperación Día 02

Auditoría, IEmpleadoRepository con adaptador y DI, mensaje correcto de dos roles, protección de migraciones frente a pérdida de datos/conversión implícita, Principal con protección de identidad/secuencia y GitHub Actions. La revisión final completó el requisito pendiente de referencias USD/tipo de cambio mediante AjusteModeloTienda. El orden temporal real de esa última corrección se mantiene en Git; no se retrofecharon commits.

Cinco commits de recuperación del Día 02. No se duplicaron hitos ya desarrollados para alcanzar artificialmente quince.

## 4. Día 03 terminado

- Contratos IJwtService, IPasswordHasher e IUsuarioRepository; representaciones Usuario/Rol/UsuarioRol independientes del framework.
- Persistencia adaptada a tablas Identity existentes, manteniendo IDs, relaciones, índices únicos, cuentas y contraseñas. No hay duplicación de usuarios ni eliminación de tablas Identity.
- BCrypt costo 12 con SHA-384 previo, marcado bcrypt-sha384, para evitar truncamiento de contraseñas Unicode/largas. Nuevas cuentas y bootstrap usan BCrypt. Las cuentas anteriores establecen BCrypt al iniciar sesión con su contraseña vigente correctamente verificada por Identity; no se convierten hashes ni se asignan claves conocidas.
- JWT HS256 de dos horas con sub, empleado_id, role, jti y versión de acceso. Clave aleatoria de 64 bytes en User Secrets, sin publicar; configuración validada al arrancar.
- LoginQuery, consulta de usuario actual, AuthController y endpoints login/me. Cuentas inactivas/bloqueadas rechazadas, bloqueo por intentos incorrectos conservado.
- Validación de firma, algoritmo, emisor, audiencia, expiración sin tolerancia, estado/roles/versión actuales. Token anterior rechazado tras cambios de acceso. Empleados exige Administrador; Vendedor consulta catálogo y no obtiene costos.
- API REST pura: retirados controladores web, Razor, recursos estáticos, cookies y CSRF. Identity Core se conserva solo para persistencia/administración de cuentas.
- Swagger Authorize Bearer comprobado en Chrome. Bootstrap explícito sin credenciales predeterminadas, sin duplicar administrador inicial y con protección del último administrador activo.

Dieciséis commits de código/pruebas de Día 03 anteriores al cierre documental; el commit de cierre documental es el decimoséptimo. Total de ambas jornadas: 22 incrementos reales, sin commits vacíos.

## 5. Archivos

Principales altas: puerto/adaptador de empleados; Usuario.cs; contratos de autenticación; JwtService/JwtRegistration/BcryptPasswordHasher/UsuarioRepository; LoginQuery/GetUsuarioActualQuery; AuthController; ProductoDto; Configurar-Jwt.ps1; migraciones/SQL; pruebas AuthApi/Login/Password; workflow CI. Modificados Program, registro DI, bootstrap, UsuarioActual, modelo/configuración/commands de productos y pruebas existentes. Retirados Controllers/Web, Views, Models MVC, wwwroot, EmpleadoSignInManager y prueba JS del interceptor CSRF obsoleto.

El inventario exacto de archivos afectados aparece al final; ARCHIVOS.md contiene el repositorio vigente.

## 6. Migraciones y PostgreSQL

Conservadas InitialCreate y EmployeeProfiles. Aplicadas después de inspección y SQL revisado:

| Migración | Resultado |
| --- | --- |
| 20261110101358_ProductoPrecioVenta | Aplicada, tabla de productos vacía; bloqueo si existen precios USD por convertir |
| 20261110101430_ClienteLocal | Aplicada, clientes vacíos; bloqueo si requieren archivo/mapeo |
| 20261110101515_VentaMostrador | Aplicada, ventas/detalles/referencias vacíos; bloqueo ante historial |
| 20261110101531_AlmacenPrincipal | Aplicada; almacén Principal e identidad/secuencia protegidas |
| 20261111160010_RetirarRolAlmacenero | Aplicada solo sin usuarios ni claims asociados |
| 20261111160613_AjusteModeloTienda | Aplicada; dos columnas nullable y restricción, sin modificar filas previas |

Inspección final: ocho migraciones aplicadas, ninguna pendiente. Un usuario y un empleado originales, roles Administrador/Vendedor, un almacén Principal; ningún dato de prueba persistente. La cuenta original conserva el hash anterior hasta su primer login correcto; no se utilizó su contraseña para probar. No se ejecutaron Down, resets, recreación de bases ni borrado de cuentas. Las transacciones de pruebas revierten sus datos; las secuencias pueden avanzar con rollback.

## 7. Endpoints

| Método | Ruta | Permiso |
| --- | --- | --- |
| POST | /api/auth/login | Anónimo |
| GET | /api/auth/me | Autenticado activo |
| GET | /api/categorias, /api/categorias/{id} | Administrador/Vendedor |
| POST | /api/categorias | Administrador |
| PUT, DELETE | /api/categorias/{id} | Administrador |
| GET | /api/productos, /api/productos/{id} | Administrador/Vendedor |
| POST | /api/productos | Administrador |
| PUT, DELETE | /api/productos/{id} | Administrador |
| GET, POST | /api/empleados | Administrador |
| GET, PUT | /api/empleados/{id} | Administrador |
| PATCH | /api/empleados/{id}/estado | Administrador |
| PUT | /api/empleados/{id}/roles | Administrador |
| GET | /api/empleados/roles | Administrador |

Swagger solo en Development: /swagger y /swagger/v1/swagger.json. Sin frontend, raíz/rutas MVC responden 404. Crear 201; modificar/eliminar 204; errores 400/401/403/404/409 según corresponda.

## 8. Validaciones ejecutadas

- Restore de la solución correcto.
- Build Release: cero errores y cero advertencias.
- Suite final Release con IMPORTNET_POSTGRES_TESTS=1 e IMPORTNET_BROWSER_TESTS=1: **32 aprobadas, cero fallidas y cero omitidas**.
- PostgreSQL real con Npgsql/Kestrel y rollback: login, ambos roles, empleados, bootstrap protegido y CRUD catálogo, incluyendo costo USD/tipo de cambio.
- Chrome headless: POST login, botón Authorize, GET me/empleados y operaciones de categorías/productos; ejecutado tanto con SQLite efímero como dentro de la prueba transaccional PostgreSQL del Administrador.
- Login legado rehash a BCrypt comprobado con cuenta temporal, sin modificar la cuenta real. Contraseña incorrecta/inactivo 401, Vendedor 403, token vencido/manipulado/revocado 401. Costos no expuestos a Vendedor.
- Auditoría NuGet con dependencias transitivas: ningún paquete vulnerable reportado.
- Verificación EF del modelo y estado final de migraciones: sin cambios pendientes (comprobación final posterior a la migración).
- CI GitHub Actions confirmado en verde para los grupos publicados; la ejecución del commit documental de cierre se consulta después de publicarlo y se informa en la entrega final.

Un commit intermedio de integración Swagger tuvo CI rojo mientras se sustituían pruebas MVC; los siguientes grupos pasaron. No se presenta ese estado intermedio como una validación aprobada.

## 9. Commits

Todos usan identidad real y fechas actuales. Publicación por grupos pequeños, con fetch previo, sin force push. El commit documental que contiene este informe se identifica con git log al terminar; no es posible incluir su propio hash dentro de su contenido.

| Bloque | Hash | Cambio |
| --- | --- | --- |
| Día 02 | 017085e | Auditoría e inspección de datos |
| Día 02 | 5969844 | Puerto y adaptador de empleados |
| Día 02 | 3ed18f5 | Protección de migraciones |
| Día 02 | 37b1a29 | CI y cierre inicial |
| Día 02, revisión final | efdf6b5 | Costo USD/tipo de cambio y migración aditiva |
| Día 03 | 42933d8 | Contratos y cuentas independientes |
| Día 03 | 4aa7149 | BCrypt y prueba de no truncamiento |
| Día 03 | a43949f | Adaptador de cuentas existentes |
| Día 03 | fa6b577 | Emisión JWT |
| Día 03 | d1a6972 | Configuración Bearer/revocación |
| Día 03 | 6a6ad9b | LoginQuery y claims |
| Día 03 | 3561aa2 | Login HTTP y transición |
| Día 03 | 315f7fb | Usuario autenticado /me |
| Día 03 | 8c36573 | Swagger Bearer/pipeline REST |
| Día 03 | 8bbe53b | Retirada MVC/cookies y pruebas REST |
| Día 03 | 8466f7f | Tokens inválidos y revocación |
| Día 03 | 1a80765 | Costos solo para Administrador |
| Día 03 | d90f706 | Bootstrap seguro |
| Día 03 | e6aa098 | Rol legado y diagnóstico |
| Día 03 | 7cf296a | Versión concurrente al emitir token |
| Día 03 | 32f5f59 | Swagger en Chrome |
| Día 03 | Commit de este informe | Documentación y evidencia final |

## 10. Publicación

Los incrementos anteriores se publicaron en origin/main durante el trabajo. La comprobación final de igualdad HEAD/origin/main, limpieza y CI del último commit se realiza tras publicar este documento y se informa al entregar. No se modificó historial previo ni se detectaron cambios remotos concurrentes.

## 11. Límites reales

No se construyó Astro ni módulos de siguientes días. Los costos USD son una referencia de catálogo, no un historial de compras ni cálculo transaccional de promedio. No se implementan refresh tokens ni registro público: JWT dura dos horas. Identity Core y sus tablas permanecen deliberadamente para preservar cuentas, validaciones y transacciones; su retirada física no fue autorizada ni necesaria. El frontend deberá enviar Bearer; CORS requiere definir sus orígenes de despliegue en la próxima jornada.

No se ejecutaron carreras simultáneas de dos sesiones PostgreSQL para el último administrador: el bloqueo existe y se probaron sus reglas, sin atribuir garantías de concurrencia a SQLite. Las rutas de protección de migraciones con datos históricos detienen el avance para exigir un mapeo explícito; no se ensayó una conversión de datos productivos inexistentes. La cuenta local real solo pasará a BCrypt cuando su propietario use su contraseña vigente; no se forzó esa transición.

## Inventario de cambios respecto a 6a81905

A: creado, M: modificado, D: retirado.

```text
A	.github/workflows/dotnet.yml
A	ImportNet/docs/AjusteModeloTienda.sql
A	ImportNet/docs/DIA-02-MIGRACIONES.sql
A	ImportNet/docs/DIA-02-RECUPERACION.md
A	ImportNet/docs/DIA-03-MIGRACIONES.sql
A	ImportNet/docs/INFORME-DIAS-02-03.md
A	ImportNet/ImportNet.API/Controllers/Api/AuthController.cs
A	ImportNet/ImportNet.Application/Security/Autenticacion.cs
A	ImportNet/ImportNet.Application/UseCases/Auth/GetUsuarioActualQuery.cs
A	ImportNet/ImportNet.Application/UseCases/Auth/LoginQuery.cs
A	ImportNet/ImportNet.Application/UseCases/Productos/ProductoDto.cs
A	ImportNet/ImportNet.Domain/Entities/Usuario.cs
A	ImportNet/ImportNet.Domain/Interfaces/IEmpleadoRepository.cs
A	ImportNet/ImportNet.Infrastructure/Identity/BcryptPasswordHasher.cs
A	ImportNet/ImportNet.Infrastructure/Identity/JwtRegistration.cs
A	ImportNet/ImportNet.Infrastructure/Identity/JwtService.cs
A	ImportNet/ImportNet.Infrastructure/Identity/UsuarioRepository.cs
A	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261111160010_RetirarRolAlmacenero.cs
A	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261111160010_RetirarRolAlmacenero.Designer.cs
A	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261111160613_AjusteModeloTienda.cs
A	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261111160613_AjusteModeloTienda.Designer.cs
A	ImportNet/ImportNet.Infrastructure/Repositories/EmpleadoRepository.cs
A	ImportNet/ImportNet.Tests/AuthApiTests.cs
A	ImportNet/ImportNet.Tests/LoginTests.cs
A	ImportNet/ImportNet.Tests/PasswordTests.cs
A	ImportNet/scripts/Configurar-Jwt.ps1
D	ImportNet/ImportNet.API/Controllers/Web/CategoriasController.cs
D	ImportNet/ImportNet.API/Controllers/Web/CuentaController.cs
D	ImportNet/ImportNet.API/Controllers/Web/EmpleadosAdminController.cs
D	ImportNet/ImportNet.API/Controllers/Web/InicioController.cs
D	ImportNet/ImportNet.API/Controllers/Web/ProductosController.cs
D	ImportNet/ImportNet.API/Models/EmpleadoForm.cs
D	ImportNet/ImportNet.API/Models/Formularios.cs
D	ImportNet/ImportNet.API/Views/_ViewImports.cshtml
D	ImportNet/ImportNet.API/Views/_ViewStart.cshtml
D	ImportNet/ImportNet.API/Views/Categorias/Editar.cshtml
D	ImportNet/ImportNet.API/Views/Categorias/Index.cshtml
D	ImportNet/ImportNet.API/Views/Cuenta/Denegado.cshtml
D	ImportNet/ImportNet.API/Views/Cuenta/Login.cshtml
D	ImportNet/ImportNet.API/Views/EmpleadosAdmin/Editar.cshtml
D	ImportNet/ImportNet.API/Views/EmpleadosAdmin/Index.cshtml
D	ImportNet/ImportNet.API/Views/Inicio/Index.cshtml
D	ImportNet/ImportNet.API/Views/Productos/Editar.cshtml
D	ImportNet/ImportNet.API/Views/Productos/Index.cshtml
D	ImportNet/ImportNet.API/Views/Shared/_Layout.cshtml
D	ImportNet/ImportNet.API/wwwroot/css/site.css
D	ImportNet/ImportNet.API/wwwroot/js/swagger-auth.js
D	ImportNet/ImportNet.API/wwwroot/lib/bootstrap/bootstrap.min.css
D	ImportNet/ImportNet.API/wwwroot/lib/bootstrap/LICENSE
D	ImportNet/ImportNet.Infrastructure/Identity/EmpleadoSignInManager.cs
D	ImportNet/ImportNet.Tests/swagger-auth.test.cjs
M	ImportNet/docs/ARCHIVOS.md
M	ImportNet/docs/DECISIONES.md
M	ImportNet/docs/ESTADO.md
M	ImportNet/docs/PRUEBA-MANUAL.md
M	ImportNet/ImportNet.API/Controllers/Api/ProductosController.cs
M	ImportNet/ImportNet.API/ImportNet.API.csproj
M	ImportNet/ImportNet.API/Program.cs
M	ImportNet/ImportNet.Application/Security/Autorizacion.cs
M	ImportNet/ImportNet.Application/UseCases/Empleados/EmpleadoDatos.cs
M	ImportNet/ImportNet.Application/UseCases/Productos/Commands/CreateProductoCommand.cs
M	ImportNet/ImportNet.Application/UseCases/Productos/Commands/UpdateProductoCommand.cs
M	ImportNet/ImportNet.Domain/Entities/Catalogo.cs
M	ImportNet/ImportNet.Infrastructure/Identity/AdminBootstrapper.cs
M	ImportNet/ImportNet.Infrastructure/Identity/UsuarioActual.cs
M	ImportNet/ImportNet.Infrastructure/ImportNet.Infrastructure.csproj
M	ImportNet/ImportNet.Infrastructure/Persistence/CatalogoConfiguration.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/DatabaseInspection.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261110101358_ProductoPrecioVenta.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261110101430_ClienteLocal.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261110101515_VentaMostrador.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/20261110101531_AlmacenPrincipal.cs
M	ImportNet/ImportNet.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
M	ImportNet/ImportNet.Infrastructure/ServiceRegistrationExtensions.cs
M	ImportNet/ImportNet.Tests/check_swagger_browser.py
M	ImportNet/ImportNet.Tests/EmpleadosTests.cs
M	ImportNet/ImportNet.Tests/PostgreSqlTests.cs
M	ImportNet/ImportNet.Tests/SmokeTests.cs
M	ImportNet/ImportNet.Tests/TestSupport.cs
M	ImportNet/README.md
M	README.md
```
