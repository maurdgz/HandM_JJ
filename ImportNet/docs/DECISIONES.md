# Decisiones vigentes

## Plataforma y arquitectura

- .NET 10 exclusivamente, por instrucción del propietario.
- API REST pura; Swagger solo en Development.
- Capas: API → Application + Infrastructure; Infrastructure → Application + Domain; Application → Domain. Domain sin dependencias externas.
- Casos de uso con MediatR (`record` + `Handler` interno). Los controladores no consultan EF.
- Repositorios y UnitOfWork para catálogo, terceros, compras e inventario; empleados usan un puerto específico (`IEmpleadoRepository`). Las consultas EF permanecen en Infrastructure.

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


## Recepción e inventario: Día 08

- La recepción exige Administrador y empleado activo. El autor se obtiene
  de la sesión, sin aceptar un EmpleadoId enviado por el cliente.
- La compra cambia de Borrador a Recibida dentro de la misma transacción
  que registra entradas, cantidades y costos promedio.
- Todos los repositorios de la recepción comparten un AppDbContext.
  La transacción utiliza aislamiento Serializable.
- Cada línea genera una Entrada en Almacen.PrincipalId, vinculada
  a CompraId, ProveedorId y EmpleadoId.
- Se conserva el control de concurrencia de PostgreSQL mediante xmin.
  Los conflictos se devuelven como 409; no se reintenta automáticamente.
- Un índice único filtrado protege CompraId, ProductoId y AlmacenId
  para movimientos de tipo Entrada con CompraId no nulo.
- El costo promedio ponderado utiliza el TotalPen persistido de la línea
  y se redondea a cuatro decimales con AwayFromZero. No se reconstruye
  a partir de unitarios redondeados para presentación.
- La recepción no cambia PrecioVenta ni las referencias históricas
  CostoImportacionUsd y TipoCambioImportacion del producto.
- Una compra recibida no permite nueva recepción, edición ni eliminación.
- Administrador y Vendedor consultan stock mediante la política CatalogoLeer.
  La política Inventario conserva su alcance de Administrador.
- La respuesta de stock omite costos y tipos de cambio para ambos roles.
- La consulta incluye productos sin inventario con cantidad cero y conserva
  productos inactivos con su indicador de estado.
- Stock bajo significa Cantidad menor o igual a StockMinimo.
- Las pruebas ordinarias de integración usan SQLite temporal. La concurrencia
  real se comprueba mediante pruebas PostgreSQL explícitamente habilitadas.