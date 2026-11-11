# Recuperación Día 02 — 11/11/2026

## Auditoría inicial

GitHub y checkout limpio en main: `6a81905674ac1d026ca744f398e8c856454dd0a5`, comprobado mediante fetch. Identidad Git existente conservada.

Ya implementado: puertos específicos de categorías/productos, adaptadores LINQ ordenados y sin seguimiento para listados, UnitOfWork con guardado atómico y traducción de conflictos PostgreSQL; handlers sin EF ni IQueryable. Dependencias: Domain independiente; Application → Domain; Infrastructure → Application/Domain; API → Application/Infrastructure. Todos los proyectos usan net10.0.

`dbc779f` limita roles a Administrador/Vendedor; `7aebed6` incorpora precio/costo PEN, stock mínimo, código único, cliente local y venta de mostrador; `6a81905` incorpora repositorios específicos del catálogo. Se conservan estos avances.

Pendientes reales: puerto de lectura de empleados en Domain, coherencia de documentación y mensajes de roles, CI y revisión de migraciones. La administración de cuentas ya posee un contrato específico en Application y transacciones con bloqueo PostgreSQL para proteger al último administrador: no corresponde sustituirlo por CRUD genérico.

PostgreSQL accesible mediante User Secrets: InitialCreate/EmployeeProfiles aplicadas, un usuario y un empleado; cuatro migraciones pendientes. ClienteLocal elimina columnas y VentaMostrador elimina tablas: requieren inspección de filas antes de aplicar. Renombrar PrecioUsd no convierte USD a PEN: si existen productos se necesita decidir precios reales, sin inventar tipo de cambio.

No se implementan compras, recepción, ventas ni frontend en esta recuperación. Se informará el número real de commits sin duplicar hitos ya resueltos.

## Cierre

Puerto IEmpleadoRepository implementado y consumido por el control de acceso activo. Se conserva la transacción especializada de administración. Corregido mensaje de roles y agregado CI restore/build/test .NET 10.

Las tablas Producto, Cliente, OrdenVenta, DetalleOrdenVenta, Almacen y MovimientoInventario estaban vacías. Se revisó `DIA-02-MIGRACIONES.sql` y se aplicaron las cuatro migraciones pendientes. Se agregaron bloqueos explícitos ante datos que requieran conversión o archivo, y protección de identidad/secuencia de almacén. Ninguna cuenta ni empleado se eliminó. No hace falta una migración duplicada AjusteModeloTienda.

La configuración de importaciones USD/tipo de cambio sigue fuera del modelo actual de compras: no se inventa un módulo ni una conversión monetaria. PrecioVenta y CostoPromedio son PEN; Codigo es el SKU único. El costo no debe exponerse al vendedor durante la transición API.

Validación inicial: build sin advertencias, 28 pruebas aprobadas y 2 PostgreSQL omitidas. Las pruebas con cookies necesitan ejecutarse fuera del sandbox Windows por DPAPI; el fallo inicial fue ambiental y se repitieron satisfactoriamente. Las verificaciones PostgreSQL y final se registran después de ejecutarlas.
