# Día 01 — 07/11/2026: empleados, perfiles y autorización

## Implementación

- Empleado conserva Id, UsuarioId, Nombre, Cargo, Area y Activo; añade Apellidos y DocumentoIdentidad. Correo, teléfono y credencial se mantienen en Identity, sin duplicarlos en el dominio.
- Creación de cuenta y empleado, edición de datos/correo/teléfono, búsqueda por nombre/apellido/documento/correo, filtro de estado, asignación de uno o más de los tres roles y activación/desactivación. No existe DELETE ni borrado físico en este módulo.
- Nombre, apellidos, documento, correo, teléfono, cargo y área obligatorios al registrar/editar; documento normalizado de 5–20 letras/números/guiones; correo y documento únicos; asociación UsuarioId única y FK restrictiva. Contraseña inicial validada por Identity; no se devuelve ni persiste en el dominio.
- Los registros antiguos sin documento se conservan con NULL y apellidos vacíos, sin inventar datos. La edición exige completar el perfil. El bootstrap de nuevas cuentas administrativas exige el perfil completo.
- Commands/Queries de MediatR con record y Handler en el mismo archivo. DTO seguro de salida sin UserManager/IdentityUser, hashes, sellos o contraseña. El contrato específico IAdministracionEmpleados permite guardar empleado y cuenta atómicamente; IUsuarioActual comprueba autorización en los casos de uso. No se añadió CRUD genérico.

## Seguridad y atomicidad

- Controladores MVC/API con política Administracion; solo Administrador. Vendedor y Almacenero no administran empleados ni roles. Los casos de uso comprueban además la identidad y la condición de administrador activo en la base, sin confiar únicamente en claims antiguos.
- SignInManager de Identity comprueba que haya un empleado asociado y activo antes de permitir login. No hay autenticación paralela.
- Cambios de acceso actualizan SecurityStamp. Identity valida las cookies con intervalo de un minuto: una sesión deshabilitada se rechaza en la siguiente petición al vencer ese intervalo. Los casos de uso de empleados rechazan de inmediato al administrador desactivado o degradado, aunque la cookie aún contenga claims anteriores.
- La comprobación del último Administrador activo y el cambio de acceso se ejecutan en la misma transacción. PostgreSQL usa pg_advisory_xact_lock(480071126) para serializar cambios concurrentes, incluyendo el bootstrap. No se permite desactivar al último administrador ni quitarle ese rol, también desde una edición completa.
- Cuenta, empleado, roles y sello de seguridad se guardan en una transacción. Las pruebas con transacción externa utilizan savepoints. Índices únicos y conflictos concurrentes se traducen a 409. No se eliminaron usuarios ni empleados para corregir conflictos.
- Antiforgery obligatorio para mutaciones MVC/API; Swagger lo añade automáticamente, también en PATCH. Respuestas API 401 sin sesión y 403 sin permisos, sin redirecciones HTML. Respuestas de administración marcadas no-store.

## Endpoints reales (solo Administrador)

| Método | Ruta | Resultado |
| --- | --- | --- |
| GET | `/api/empleados?busqueda=&activo=` | Listado y filtros |
| GET | `/api/empleados/{id}` | Perfil seguro o 404 |
| POST | `/api/empleados` | Cuenta + empleado; 201 con Location |
| PUT | `/api/empleados/{id}` | Datos, roles y estado; 204 |
| PATCH | `/api/empleados/{id}/estado` | Activación/desactivación; 204 |
| PUT | `/api/empleados/{id}/roles` | Asignar roles existentes; 204 |
| GET | `/api/empleados/roles` | Administrador, Vendedor, Almacenero |

Crear recibe `{ "datos": { "nombre", "apellidos", "documentoIdentidad", "email", "telefono", "cargo", "area" }, "password", "activo", "roles" }`; editar recibe los mismos datos sin password. Los valores completos de ejemplo están en los esquemas de Swagger. El id de ruta prevalece sobre el cuerpo. UsuarioId nunca se acepta para reasociar una cuenta arbitraria.

## Pantallas

- `/empleados`: búsqueda, filtro de estado, roles, estado y botones de editar/activar/desactivar.
- `/empleados/crear`: datos personales/laborales, cuenta, contraseña inicial y selección de roles.
- `/empleados/{id}/editar`: perfil, asociación Identity de solo lectura, roles y estado. Sin contraseña existente.
- Formularios POST `/empleados/guardar` y `/empleados/{id}/estado`, con antiforgery, errores visibles y mensajes de confirmación. Menú de empleados solo para Administrador; la protección también está en el servidor.

## PostgreSQL y migraciones

Se encontró la conexión en User Secrets y se inspeccionó ImportNet como postgres. Antes de aplicar había cero tablas y cero migraciones. Se revisaron los cambios hacia delante y se aplicaron correctamente:

1. `20261107010635_InitialCreate`.
2. `20261107151357_EmployeeProfiles`.

EmployeeProfiles añade dos columnas e índices únicos. Sustituye EmailIndex por su versión única; no elimina tablas ni datos. SQL revisable en `EmployeeProfiles.sql`. La inspección posterior confirmó 19 tablas y ninguna migración pendiente. No se ejecutaron métodos Down, recreación de base ni operaciones destructivas.

## Verificación

Resultado de la suite completa con PostgreSQL y navegador habilitados: **31 pruebas aprobadas, ninguna fallida ni omitida**. Build: **0 errores y 0 advertencias**. La comprobación final del modelo no detectó cambios sin migrar y la auditoría NuGet no reportó dependencias vulnerables. Se verificó posteriormente que los usuarios y empleados temporales habían sido revertidos; no quedó ningún administrador de pruebas en la base.

- Cinco casos esenciales nuevos: permisos de Vendedor/Almacenero; inactivo sin login y cookie revocada al validar SecurityStamp; último administrador protegido; creación/edición con validaciones, relaciones y formularios. Se reutilizaron las utilidades existentes.
- PostgreSQL real: los tres casos Administrador/Almacenero/Vendedor pasaron; incluyen creación/edición/roles/desactivación de empleados y regresión del catálogo con HTTP real/Kestrel y Npgsql. Los datos de prueba se revierten, sin borrar registros ajenos. Las secuencias pueden avanzar con rollback.
- Chrome headless: se reutiliza la prueba existente para Swagger y catálogo, y para registro/edición/cambio de roles/desactivación en MVC de empleados, tanto con SQLite transitorio como con PostgreSQL real. No guarda contraseñas, capturas ni trazas.
- La protección del último administrador se verificó en los casos esenciales. No se ejecutó una prueba de carrera con dos sesiones PostgreSQL simultáneas; el bloqueo transaccional está implementado, sin atribuir a SQLite garantías de concurrencia PostgreSQL.

## Primer acceso y reproducción

La prueba usa cuentas temporales y rollback; no deja una contraseña conocida ni un administrador oculto. Para crear tu cuenta persistente, ejecuta localmente:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/Crear-Administrador.ps1
dotnet run --project ImportNet.API/ImportNet.API.csproj --launch-profile ImportNet
```

Swagger: http://localhost:5080/swagger. Login: http://localhost:5080/cuenta/login. Empleados: http://localhost:5080/empleados. El bootstrap pide tu contraseña oculta y retira Bootstrap:Password del almacén al terminar.

```powershell
dotnet build ../ImportNet.sln
dotnet test ../ImportNet.sln
# Integración real explícita:
$env:IMPORTNET_POSTGRES_TESTS = "1"
dotnet test ../ImportNet.sln --filter Database=PostgreSQL
Remove-Item Env:IMPORTNET_POSTGRES_TESTS
```

Las pruebas de navegador siguen siendo opcionales, con IMPORTNET_BROWSER_TESTS=1, Python/Playwright y Chrome. No se añadieron pruebas por cada getter, setter o propiedad. Inventario, proveedores, clientes, ventas y reportes no se desarrollaron en esta jornada.
