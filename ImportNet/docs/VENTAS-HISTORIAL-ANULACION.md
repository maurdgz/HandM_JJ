# Historial, nota de venta y anulación

Entrega original integrada por PR #19: `feat/2026-11-26-anulacion-ventas`, issue #13. Integración y pruebas adicionales en `feat/ventas-dia12`; ver [Día 12](DIA-12-VENTAS.md).

## Alcance

- Historial de ventas filtrable por fecha: el Administrador ve todas; el Vendedor solo las suyas.
- Nota de venta imprimible con número, fecha, vendedor, cliente, líneas y total.
- Anulación solo Administrador, con motivo: `Registrada → Anulada` y reposición del stock una sola vez con un movimiento de Entrada referenciado a la venta.
- No hay migración: la tabla `Venta` ya tenía `Estado`, `MotivoAnulacion`, `FechaAnulacion` y la restricción `CK_Venta_Anulacion`.

## Endpoints

| Método | Ruta | Roles | Respuesta |
|---|---|---|---|
| `GET` | `/api/ventas?desde=AAAA-MM-DD&hasta=AAAA-MM-DD&pagina=1&tamano=20` | Administrador, Vendedor | `Pagina<ResumenVenta>`, más recientes primero |
| `GET` | `/api/ventas/{id}` | Administrador, Vendedor (solo propias) | `NotaVenta` |
| `POST` | `/api/ventas/{id}/anulacion` con `{ "motivo": "..." }` | Administrador | `VentaAnuladaDto` con el stock repuesto por producto |

- Las fechas son días en hora de Perú (UTC−5), ambas inclusivas.
- El vendedor sale del token. Para el Vendedor, el filtro por empleado lo aplica el servidor; no se puede pedir el de otro.
- Errores: Vendedor que anula o abre una nota ajena → 403; venta inexistente → 404; motivo vacío o de más de 300 caracteres → 400; venta ya anulada → 409.

## Anulación

`AnularVentaCommand` delega en el puerto `IAnulacionVentas`, implementado en Infrastructure mediante una transacción y un bloqueo PostgreSQL por venta:

1. Exige Administrador activo.
2. Carga la venta; si ya está anulada, responde 409 sin tocar el stock.
3. `Venta.Anular(motivo)` cambia el estado y guarda motivo y fecha.
4. Verifica que las salidas originales coincidan con las cantidades del detalle; repone cada salida en su almacén y agrega un movimiento `Entrada` con `VentaId`, el empleado que anula y el motivo como observación. Cualquier error revierte el conjunto completo.

Las barreras contra duplicados incluyen el bloqueo transaccional por venta, la comprobación de estado y de reposición previa, el control de concurrencia (`xmin`) de `Venta` e `Inventario` y el índice único `(VentaId, ProductoId, AlmacenId, Tipo)` de `MovimientoInventario`.

En el kardex, la entrada aparece como `Anulación venta N° X`.

## Arquitectura

- Puerto `IVentaRepository` en Domain (`Historial`, `Nota`, `Obtener`, `Detalles`), expuesto como `IUnitOfWork.Ventas`.
- Adaptador `VentaRepository` en Infrastructure con LINQ: joins con empleado, cliente y producto, `Where` por fecha y empleado, `OrderByDescending`, `Skip`/`Take` y `Count`.
- `IUsuarioActual.EsAdministrador` (implementación por defecto sobre `ExigirAdministrador`) para decidir el filtro del historial.
- Ningún handler usa `DbContext` ni `IQueryable`.

## Pantallas

- `/historial`: filtro desde/hasta (por defecto hoy), paginación, estado con color, "Ver nota" y "Anular" (solo Administrador, en ventas registradas). El Vendedor no ve la columna Vendedor.
- `/nota?id=N`: nota con número `NV-000001`, cliente con documento, vendedor, líneas, subtotal, descuento y total. Si está anulada muestra el sello con fecha y motivo. Botón Imprimir; al imprimir se ocultan el menú, los botones y los avisos.
- El POS enlaza a la nota de la venta recién registrada.

## Verificación

Sobre una base PostgreSQL nueva con la API levantada:

- Conteo físico de 10 → venta del Administrador de 3 → venta del Vendedor de 2.
- Historial: el Administrador ve 2 ventas, el Vendedor solo la suya. El filtro de hoy devuelve 2 y el de ayer 0.
- Nota de la venta 1: subtotal 136.50, descuento 1.50, total 135.00, cliente "Cliente varios".
- Vendedor: nota ajena 403, nota propia 200, anular 403.
- Motivo vacío 400. Anulación 200 con stock 5 → 8. Segunda anulación 409. Venta inexistente 404.
- Kardex: +10 / −3 / −2 / +3 (`Anulación venta N° 1`), saldo 8 igual al stock.
- `dotnet test`: 142 aprobadas, 6 omitidas, 0 fallidas (sin pruebas nuevas en esta entrega).
- `npm run build` y `astro check` sin errores.

## Pruebas añadidas en la integración

- Historial por propietario y fecha, paginación, nota, contrato de anulación, permisos y validación.
- Rollback ante fallo de persistencia y dos anulaciones simultáneas contra PostgreSQL aislado.
- Chrome para historial, diálogo, nota, estilos de impresión y sesión. Comandos y evidencia en [Día 16](DIA-16-SEGURIDAD.md).
