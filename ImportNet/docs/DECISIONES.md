# Decisiones vigentes

## Plataforma y arquitectura

- .NET 10 exclusivamente, por instrucción del propietario.
- API REST pura; Swagger solo en Development.
- Capas: API → Application + Infrastructure; Infrastructure → Application + Domain; Application → Domain. Domain sin dependencias externas.
- Casos de uso con MediatR (`record` + `Handler` interno). Los controladores no consultan EF.
- Repositorio y UnitOfWork solo para Categoría y Producto; empleados usan un puerto específico (`IEmpleadoRepository`).

## Autenticación y permisos

- JWT HS256 de dos horas, sin tolerancia de reloj. Clave aleatoria en User Secrets; no hay clave predeterminada.
- Cada petición comprueba empleado activo, roles y versión de acceso; cambiar estado, roles o credencial invalida tokens anteriores.
- Roles: Administrador y Vendedor. El Vendedor consulta catálogo pero no ve costos ni tipo de cambio.
- Se protege transaccionalmente al último Administrador activo. El bootstrap inicial no usa credenciales predeterminadas.
- Contraseñas con BCrypt costo 12 y prehash SHA-384 (`{bcrypt-sha384}`). Las cuentas con hash de Identity pasan a BCrypt solo al iniciar sesión con su contraseña correcta.
- Identity Core se mantiene solo como adaptador de almacenamiento de cuentas; Usuario, Rol y UsuarioRol se proyectan desde las tablas `AspNet*` existentes.

## Modelo

- `Codigo` es el SKU único del producto. `PrecioVenta` y `CostoPromedio` en PEN.
- `CostoImportacionUsd` y `TipoCambioImportacion` (PEN por USD) son referencias opcionales y conjuntas; no convierten precios.
- Dinero `decimal(18,2)`, costo promedio `decimal(18,4)`, tipo de cambio `decimal(18,6)`, fechas en UTC.
- Stock por producto y almacén; no es editable en Producto. Movimientos inmutables: se corrigen con un nuevo ajuste.
- Relaciones restrictivas para conservar historial. No se borra físicamente a ningún empleado.
- Concurrencia optimista con `xmin` de PostgreSQL.

## Datos y pruebas

- Las migraciones bloquean cambios que perderían datos o requerirían conversión implícita.
- Los scripts SQL no se versionan: se generan con `dotnet ef migrations script --idempotent`.
- Pruebas ordinarias con SQLite en memoria; las de PostgreSQL se activan de forma explícita y revierten sus datos.
