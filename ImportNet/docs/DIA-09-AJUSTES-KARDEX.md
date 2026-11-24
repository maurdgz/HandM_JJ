# Día 09: ajustes de inventario y kardex

Responsable: Huber Lope Tapara.
Fecha de implementación: 24/11/2026.
Fecha prevista en el plan: 17/11/2026.
Rama: `feat/2026-11-24-ajustes-kardex` (parte de `feat/dia-11-punto-de-venta`, que ya incluye la recepción del Día 08).

## Alcance

- Ajustes de inventario con motivo: merma, rotura, conteo físico y devolución a proveedor.
- Kardex por producto con saldo inicial, saldo acumulado y referencia a compra, venta o ajuste.
- Movimientos inmutables: un error se corrige con un ajuste nuevo, nunca editando ni borrando.
- Pantalla Kardex y ajustes (solo Administrador) y enlace "Kardex" desde Stock y alertas.

## Reglas de ajuste

`POST /api/inventario/ajustes` con `{ productoId, motivo, cantidad, observacion? }`.

| Motivo | `cantidad` significa | Movimiento registrado |
|---|---|---|
| `Merma`, `Rotura`, `DevolucionProveedor` | unidades que salen | `-cantidad` |
| `ConteoFisico` | unidades contadas en el almacén | `contado - stock actual` |

- La cantidad admite hasta 3 decimales. El conteo puede ser 0; los demás motivos exigen una cantidad positiva.
- Se rechaza (400) un ajuste que dejaría stock negativo, y también un conteo igual al stock porque no habría diferencia.
- La observación es opcional, hasta 300 caracteres.
- El empleado sale del token. Si otra operación cambia ese stock al mismo tiempo, el ajuste se rechaza con 409 (transacción serializable y columna de versión).
- Solo Administrador. El Vendedor recibe 403 y un usuario anónimo, 401.

## Kardex

`GET /api/inventario/kardex/{productoId}?desde=AAAA-MM-DD&hasta=AAAA-MM-DD`

- Las fechas son días en hora de Perú (UTC−5) y ambas son inclusivas.
- Los movimientos anteriores a `desde` forman el saldo inicial.
- Cada línea trae entrada, salida, saldo acumulado, empleado, motivo y observación.
- La consulta usa LINQ en `MovimientoRepository.Kardex`: join con empleado y venta, filtro por fecha y orden por fecha e Id. El saldo acumulado se calcula sobre la lista ordenada, porque SQLite no suma decimales en el servidor.
- Sin fecha final, `saldoFinal` debe ser igual a `stockActual`; la pantalla muestra "Cuadra" o "No cuadra".

## Persistencia

Migración `AjustesInventarioKardex`:

- Columnas `MotivoAjuste` y `Observacion` en `MovimientoInventario`.
- Restricción `CK_Movimiento_MotivoAjuste`: todo ajuste lleva motivo y solo los ajustes lo llevan.
- Índice `IX_Movimiento_ProductoFecha` para el kardex.
- Solo en PostgreSQL: trigger `TR_Movimiento_Inmutable`, que bloquea `UPDATE` y `DELETE` incluso con `ExecuteUpdate`, `ExecuteDelete` o SQL directo. En EF, `AppDbContext` ya lo bloqueaba.

## Verificación

- `dotnet test`: 142 aprobadas, 6 omitidas (PostgreSQL y navegador), 0 fallidas. Son 28 pruebas nuevas en `AjustesKardexTests.cs`.
- Flujo por la API: compra de 10 → recepción → venta de 3 → merma de 2 → conteo de 6. El kardex da +11 / −5, saldo 6, igual al stock.
- La migración se aplicó en una base PostgreSQL nueva. Ahí se probaron por la API la compra, la recepción, la venta, el ajuste y el ajuste negativo (400). Con SQL directo, el trigger rechazó `UPDATE` y `DELETE`.
- En el navegador, la pantalla de kardex mostró los movimientos y el cuadre. El diálogo de ajuste no se pudo abrir con la automatización del navegador; el diálogo existente de Categorías tampoco abrió en esa sesión. **Pendiente: probar a mano el diálogo de ajuste.**
- `npm run build` y `astro check` sin errores.
