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

## Pendiente

- Pruebas automáticas de reportes con datos sembrados (quedan para el QA).
- Pantallas de reportes y exportación a Excel (issue #15).
