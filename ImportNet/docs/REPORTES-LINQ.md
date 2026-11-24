# Reportes con LINQ

Rama: `feat/2026-11-26-reportes-linq`. Issue #14.

## Endpoints

Todos en `/api/reportes`. Fechas `AAAA-MM-DD` en hora de Perú (UTC−5), ambas inclusivas. Sin fechas: el mes en curso hasta hoy. Solo cuentan ventas **Registradas**.

| Ruta | Roles | Parámetros | Devuelve |
|---|---|---|---|
| `ventas-por-periodo` | Admin | `desde`, `hasta`, `agrupacion=Dia\|Mes` | `[{ periodo, ventas, total }]` ordenado por periodo |
| `ventas-por-vendedor` | Admin | `desde`, `hasta` | `[{ empleadoId, vendedor, ventas, total, ticketPromedio }]` |
| `top-productos` | Admin | `desde`, `hasta`, `top` (1–50, por defecto 10) | `[{ productoId, codigo, nombre, unidadMedida, cantidad, total }]` por cantidad |
| `margen-productos` | Admin | `desde`, `hasta` | `[{ …, cantidad, ingreso, costoPromedio, costo, margen, margenPorcentaje }]` |
| `stock-bajo` | Admin | — | productos activos con stock ≤ mínimo, por mayor faltante |
| `cierre-caja` | Admin | `fecha` (por defecto hoy) | totales del día, anuladas aparte y desglose por vendedor |
| `mi-resumen` | Admin, Vendedor | `fecha` (por defecto hoy) | igual que `cierre-caja`, solo con las ventas del usuario del token |
| `compras-por-proveedor` | Admin | `desde`, `hasta` | compras **Recibidas**: `[{ proveedorId, proveedor, pais, compras, totalUsd, totalPen }]` |

Errores 400: agrupación distinta de `Dia`/`Mes`, `top` fuera de 1–50, `desde` posterior a `hasta`, rango mayor a 366 días (1100 si se agrupa por mes). El Vendedor recibe 403 en todo salvo `mi-resumen`; un usuario anónimo, 401.

## Reglas de cálculo

- **Ventas:** suma de `Venta.Total` (ya neto de descuentos). Las anuladas no entran en ningún total; en el cierre se informan aparte (`anuladas`, `montoAnulado`).
- **Top y margen:** suma de `DetalleVenta.Subtotal` (neto del descuento de línea), así que el total de los productos coincide con el de las ventas.
- **Margen:** `ingreso − cantidad × CostoPromedio`. La venta no guarda el costo del momento, por lo que se usa el **costo promedio vigente** del producto. Si una recepción posterior cambia el promedio, el margen histórico cambia con él.
- **Compras:** USD = Σ cantidad × costo USD; PEN = Σ mercadería + flete + aduana prorrateados. Los borradores se excluyen.
- **Periodos:** la fecha UTC se lleva a hora de Perú antes de agrupar por día o mes.

## Arquitectura

- Puerto `IReporteRepository` en Domain, expuesto como `IUnitOfWork.Reportes`. DTOs en `Domain/Entities/Reportes.cs`.
- Adaptador `ReporteRepository` en Infrastructure. `Where`, joins (`join … into … DefaultIfEmpty` para stock sin inventario) y `Select` se ejecutan en la base. `GroupBy`, `Sum`, `Count`, `OrderByDescending` y `Take` se aplican sobre las filas proyectadas, porque SQLite (base de las pruebas) no suma decimales en el servidor; es el mismo criterio del kardex.
- Queries MediatR en `Application/UseCases/Reportes`. Cada handler exige Administrador además de la política del controlador; `mi-resumen` toma el empleado del token.
- `PeriodoReporte` convierte días de Perú a un rango UTC `[desde, hasta)`.

## Índices

Migración `IndicesReportes`:

- `IX_Venta_EstadoFecha (Estado, Fecha)`: ventas Registradas por periodo.
- `IX_Venta_EmpleadoFecha (EmpleadoId, Fecha)`: resumen diario por vendedor. Reemplaza a `IX_Venta_EmpleadoId`, que queda cubierto.
- `IX_Compra_EstadoFecha (Estado, Fecha)`: compras Recibidas por periodo.

## Verificación

Base PostgreSQL nueva con la migración aplicada y la API levantada. Datos:

- Compra recibida de Yiwu Home Co: olla 10 × 5 USD y sartén 8 × 3 USD, tipo de cambio 3.80, flete 38, aduana 19. Costo promedio: olla 22.852, sartén 13.71. Otra compra en borrador.
- Ventas de hoy: Admin olla 3 con descuento 1.50 (135.00); Vendedora olla 2 + sartén 1 (121.00); Admin sartén 4 (120.00, luego anulada).

| Reporte | Esperado (a mano) | API |
|---|---|---|
| Ventas por día y por mes | 2 ventas, 256.00 | ✓ |
| Ventas por vendedor | Admin 1 / 135.00; Vendedora 1 / 121.00 | ✓ |
| Top productos | olla 5 / 226.00; sartén 1 / 30.00 | ✓ |
| Margen olla | 226.00 − 5 × 22.852 = 111.74 (49.44 %) | ✓ |
| Margen sartén | 30.00 − 13.71 = 16.29 (54.30 %) | ✓ |
| Stock bajo | juguete 0/3 (faltan 3); sartén 3/5 (faltan 2) | ✓ |
| Cierre de caja | 2 ventas, subtotal 257.50, descuento 1.50, total 256.00; 1 anulada por 120.00 | ✓ |
| Mi resumen (Vendedora) | 1 venta, 121.00 | ✓ |
| Compras por proveedor | 1 compra, 74.00 USD, 338.20 PEN (borrador excluido) | ✓ |
| Cierre de ayer | 0 ventas | ✓ |

- Vendedor en los 7 reportes de Administrador: 403. Anónimo: 401. Validaciones de agrupación, top, rango invertido y rango largo: 400.
- La venta se anuló con `POST /api/ventas/{id}/anulacion`.
- `dotnet test`: 142 aprobadas, 6 omitidas, 0 fallidas (sin pruebas nuevas en esta entrega).

## Pantalla de reportes y descarga a Excel (issue #15)

Entra como **Administrador**, abre **Reportes** en el menú, elige un reporte y aplica las fechas. Ventas por periodo permite agrupar por día o mes, Productos más vendidos permite cambiar el máximo, Cierre de caja solicita un día y Stock bajo muestra el inventario actual.

**Descargar Excel** usa los filtros aplicados a la tabla. Los encabezados, valores y totales se construyen a partir de las consultas LINQ. El libro muestra los filtros en el título, con moneda PEN donde corresponde y USD separado para compras. El endpoint de descarga responde 403 al Vendedor y 401 a una sesión anónima.

- `GET /api/reportes/{reporte}/tabla` devuelve columnas, filas y totales para la pantalla.
- `GET /api/reportes/{reporte}/excel` devuelve el libro XLSX con los mismos parámetros.
- `dotnet test ImportNet.sln -c Release --filter FullyQualifiedName~ReportesExcelTests` verifica las cifras, los encabezados, el formato y los permisos.
- Con Chrome, Playwright para Python y el frontend instalado mediante `npm ci`, ejecutar `IMPORTNET_BROWSER_TESTS=1 dotnet test ImportNet.sln -c Release --filter FullyQualifiedName~ReportesBrowserTests` para verificar la descarga desde Astro.

## Integración del dashboard (Día 15, issue #16)

- `GET /api/reportes/dashboard`: KPI administrativos de hoy/mes en PEN, ticket, margen al costo vigente y conteos de inventario. Protegido en controlador y handler; el Vendedor utiliza únicamente `mi-resumen`.
- PostgreSQL ejecuta ahora `GroupBy`, `Sum`, `Count`, orden y límite de ventas por periodo, vendedores y top en el servidor. Margen agrupa en servidor y redondea por producto como antes. Se mantiene la alternativa SQLite para las pruebas existentes.
- Los tres agregados del KPI comparten una transacción de lectura RepeatableRead para evitar diferencias si se registra una venta durante la consulta. Sin cambios al esquema ni al contrato de los reportes anteriores.
- `DashboardTests` y `DashboardPostgreSqlTests` verifican las agregaciones, anuladas, descuentos y fronteras UTC-5. La navegación y los permisos se validan con Chrome contra PostgreSQL aislado.
- La pantalla de Inicio usa los indicadores y gráficos del dashboard. El menú Reportes ofrece las tablas y exportaciones del issue #15.
