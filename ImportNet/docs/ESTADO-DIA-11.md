# Estado Día 11 — POS

Rama: `feat/dia-11-punto-de-venta`. Base verificada: `0baef9cc2592c7e46a1478ccffddbb308d4cc43b`.

## Días previos

- Día 08: pendiente. Hay entidades `Inventario`, `MovimientoInventario` y `Compra`, pero no `RecibirCompraCommand`, ni flujo API/UI de recepción.
- Día 09: pendiente. No se encontraron comandos de ajuste, consulta kardex o pantallas de inventario.
- Día 10: parcial. Existen pruebas PostgreSQL para auth/catálogo (`PostgreSqlTests.cs`), pero no el recorrido compras→recepción→ajuste→kardex ni evidencia de migración nueva/CI para ese recorrido.

## POS

Reglas de dominio PEN, contrato/handler MediatR, adaptador `RegistroVenta` y endpoint `POST /api/ventas` implementados en esta continuación. La operación agrupa productos repetidos, valida actividad/cliente/cantidad/descuento/stock, toma precio del catálogo, modifica inventario con xmin de EF/Npgsql y guarda venta, detalle y salidas dentro de una transacción. El acceso está protegido por la política `Ventas` y resuelve empleado activo desde JWT. El descuento está limitado provisionalmente a 50% por línea agregada y aún requiere configuración.

Verificación actual: `dotnet build ImportNet.sln --configuration Release` pasó con 0 warnings y errores. `dotnet test ... --filter FullyQualifiedName~SmokeTests`: 3 pasaron, 1 omitida. La corrida general anterior reportó 49 pasaron, 5 omitidas y 15 fallidas, principalmente por falta de registro del adaptador, ya corregida; también falló `ComprasApiTests.Api_ValidacionesNoDejanComprasParciales`, pendiente de repetir/investigar. PostgreSQL, pruebas transaccionales POS, UI Astro y CI aún no verificados.

La rama del compañero `origin/feat/2026-11-20-recepcion-inventario` contiene 12 commits desde `main` (SHA final `843fdef`) y no es ancestro de `origin/main`; no se integró. El flujo POS usa el modelo `Inventario` existente en main para evitar importar su rama completa. Todavía faltan UI Astro y pruebas POS/PostgreSQL.

## Continuación

Completar repositorios/caso de uso de venta con reserva de stock concurrente y transacción PostgreSQL; agregar API y pruebas PostgreSQL; luego isla Astro y su verificación. Mantener ventas y cobros exclusivamente en PEN. No implementar historial, impresión ni anulación.
