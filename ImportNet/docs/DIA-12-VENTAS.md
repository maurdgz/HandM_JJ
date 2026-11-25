# Día 12 Historial nota de venta y anulación

Responsable: José Luis Tincopa Paz. Entrega sobre el POS, recepción e inventario ya integrados.

## Funcionalidad

- `/ventas` conserva el punto de venta.
- `/ventas/historial` consulta por fechas de Perú. Administrador ve todas las ventas; Vendedor solo las de su empleado autenticado.
- `/ventas/nota?id=...` muestra número, fecha, vendedor, cliente, líneas e importes del servidor. Imprimir oculta navegación y botones; una venta anulada se identifica expresamente.
- `GET /api/ventas?desde=AAAA-MM-DD&hasta=AAAA-MM-DD` incluye ambos días. Sin filtros se consultan los últimos 30 días; el periodo máximo es 366 días.
- `GET /api/ventas/{id}` devuelve cabecera y líneas. Una venta ajena al Vendedor responde 404, sin revelar sus datos.
- `POST /api/ventas/{id}/anulacion`, con `{"motivo":"..."}`, requiere Administrador. Anónimo recibe 401; Vendedor 403. Motivo obligatorio de hasta 300 caracteres. Repetir la anulación devuelve 409.

## Integridad

La anulación usa una transacción y, en PostgreSQL, un bloqueo transaccional por venta. Cambia Registrada a Anulada, repone las cantidades de sus salidas en los almacenes originales y registra entradas nuevas con VentaId y empleado responsable. Las salidas anteriores permanecen inmutables. Se comprueba la coincidencia con el detalle antes de reponer; no se modifica el costo promedio. Un fallo revierte venta, stock y entradas.

Los filtros se aplican a las entidades antes de proyectar el modelo de lectura para que EF traduzca la consulta. Las fechas se devuelven en UTC también con SQLite y se muestran en America/Lima.

## Pruebas

`VentasDia12Tests` verifica el recorrido POS → historial → nota → anulación, propiedad del Vendedor, permisos, motivos inválidos, días peruanos, rechazo de duplicados y rollback ante fallo de persistencia, seguido de un reintento correcto.

`VentasConcurrenciaPostgreSqlTests` aplica las migraciones en un esquema temporal propio y ejecuta dos anulaciones simultáneas con conexiones independientes. Comprueba una respuesta 204, otra 409 y una única reposición de cada línea. El esquema se elimina al terminar; no se cambian las tablas públicas del negocio.

Desde la raíz:

```powershell
dotnet test ImportNet.sln --configuration Release
$env:IMPORTNET_VENTAS_POSTGRES_TESTS = '1'
dotnet test ImportNet.sln --configuration Release --filter 'FullyQualifiedName~VentasConcurrenciaPostgreSqlTests'
Remove-Item Env:IMPORTNET_VENTAS_POSTGRES_TESTS
npm.cmd --prefix ImportNet/frontend run check
npm.cmd --prefix ImportNet/frontend run build
```

La prueba PostgreSQL usa la conexión local de User Secrets o `ConnectionStrings__ImportNet`, exclusivamente en un servidor de loopback. El workflow .NET prepara un contenedor de PostgreSQL para ejecutarla en CI. La prueba opcional de navegador y la publicación en un solo host se comprueban junto a la entrega del Día 16.
