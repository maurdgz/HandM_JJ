# Estado Día 11 — POS

Rama: `feat/dia-11-punto-de-venta`. Base verificada: `0baef9cc2592c7e46a1478ccffddbb308d4cc43b`.

## Días previos

- Día 08: pendiente. Hay entidades `Inventario`, `MovimientoInventario` y `Compra`, pero no `RecibirCompraCommand`, ni flujo API/UI de recepción.
- Día 09: pendiente. No se encontraron comandos de ajuste, consulta kardex o pantallas de inventario.
- Día 10: parcial. Existen pruebas PostgreSQL para auth/catálogo (`PostgreSqlTests.cs`), pero no el recorrido compras→recepción→ajuste→kardex ni evidencia de migración nueva/CI para ese recorrido.

## POS

Reglas de dominio PEN, contrato/handler MediatR, adaptador `RegistroVenta` y endpoint `POST /api/ventas` implementados en esta continuación. La operación agrupa productos repetidos, valida actividad/cliente/cantidad/descuento/stock, toma precio del catálogo, modifica inventario con xmin de EF/Npgsql y guarda venta, detalle y salidas dentro de una transacción. El acceso está protegido por la política `Ventas` y resuelve empleado activo desde JWT. El descuento está limitado provisionalmente a 50% por línea agregada y aún requiere configuración.

Verificación actual: `dotnet build ImportNet.sln --configuration Release` pasó (0 warnings, 0 errores). La suite completa pasó: 64 aprobadas, 5 omitidas, 0 fallidas. `VentasDominioTests`: 5 aprobadas. Smoke tests: 3 aprobadas, 1 omitida. Astro check y build pasaron (10 hints preexistentes). PostgreSQL POS y prueba real desde navegador todavía no están verificadas. El test POS actual cubre dominio; aún faltan pruebas transaccionales para stock, rollback y carreras.

La rama del compañero `origin/feat/2026-11-20-recepcion-inventario` contiene 12 commits desde `main` (SHA final `843fdef`) y no es ancestro de `origin/main`; no se integró. El POS usa el modelo `Inventario` disponible en main. UI Astro implementada; falta la prueba funcional de registro y pruebas PostgreSQL de concurrencia/rollback.

## Continuación

Completar repositorios/caso de uso de venta con reserva de stock concurrente y transacción PostgreSQL; agregar API y pruebas PostgreSQL; luego isla Astro y su verificación. Mantener ventas y cobros exclusivamente en PEN. No implementar historial, impresión ni anulación.
