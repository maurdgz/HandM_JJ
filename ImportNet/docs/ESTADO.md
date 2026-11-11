# Estado — 11/11/2026 (America/Lima)

ASP.NET Core Web API pura en .NET 10, PostgreSQL y cuatro capas. El checkout inicial estaba limpio y coincidía con GitHub en 6a81905. Se conservan los avances de dbc779f, 7aebed6 y 6a81905.

Implementados: catálogo CRUD, empleados/perfiles/roles, JWT de dos horas, BCrypt con transición verificada desde Identity, /api/auth/login, /api/auth/me, permisos de Administrador/Vendedor, revocación por estado/roles/versión, protección del último administrador y bootstrap local seguro. Swagger con Authorize Bearer. Sin MVC ni cookies. Las tablas de Identity se conservan; Identity Core administra su almacenamiento desde Infrastructure.

Modelo: venta y costo promedio en PEN; SKU único; stock mínimo; referencias opcionales de costo USD y tipo de cambio PEN/USD; clientes locales; almacén Principal. No se implementaron compras, recepción, ventas transaccionales, reportes ni Astro.

PostgreSQL: migraciones iniciales, cuatro ajustes anteriores, RetirarRolAlmacenero y AjusteModeloTienda aplicados. Un usuario y un empleado originales conservados. Almacenero retirado solo tras comprobar ausencia de asignaciones. Cuenta original con hash legado conservado hasta su login correcto. No se eliminaron datos de negocio ni tablas Identity.

Resultados detallados, commits y limitaciones: [INFORME-DIAS-02-03.md](INFORME-DIAS-02-03.md). Los informes anteriores describen su fecha original; para ejecutar la API usar README y PRUEBA-MANUAL actuales.
