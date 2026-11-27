# Estado

Actualizado: 27/11/2026. Referencia: plan de desarrollo v2 (periodo técnico 10–26/11/2026). Estado de la rama local `feat/ventas-dia12`, integrada con main hasta `58d4502`; los nuevos commits de Días 12 y 16 aún no se enviaron a GitHub.

| Día | Fecha | Responsable | Módulo | Estado |
| --- | --- | --- | --- | --- |
| 01 | 07/11 | Mauricio Rodríguez | Empleados y autorización por rol | Hecho |
| 02 | 10/11 | José Carlos Vitorino | Arquitectura hexagonal y ajuste del modelo a tienda | Hecho (cerrado el 11/11) |
| 03 | 11/11 | Mauricio Rodríguez | Autenticación JWT + BCrypt y API REST pura | Hecho |
| 04 | 12/11 | José Luis Tincopa | Frontend base: Astro + Tailwind + shadcn | Integrado conservando sus cinco commits |
| 05 | 13/11 | Huber Lope | Catálogo con búsqueda y paginación | Recuperado sobre el CRUD existente |
| 06 | 14/11 | José Carlos Vitorino | Proveedores y clientes | Recuperado sobre entidades y mapeos existentes |
| 07 | 15/11 | Mauricio Rodríguez | Compras de importación (borrador) y costo puesto en tienda | Implementado y probado; recepción fuera de alcance |
| 08 | 16/11 | José Luis Tincopa Paz | Recepción de compra e inventario | Integrado en main mediante PR #11 |
| 09 | 17/11 | Huber Lope | Ajustes de inventario y kardex | Integrado en main mediante PR #12 |
| 10 | 18/11 | José Carlos Vitorino | QA intermedio: compras, inventario y roles | Pendiente |
| 11 | 19/11 | Mauricio Rodríguez | Punto de venta (POS) | Integrado en main mediante PR #9 |
| 12 | 20/11 | José Luis Tincopa | Historial, nota de venta y anulación | Implementado y probado localmente; pendiente de push y PR |
| 13 | 21/11 | Huber Lope | Reportes con LINQ | Integrado en main mediante PR #20 |
| 14 | 22/11 | José Carlos Vitorino | Pantallas de reportes y exportación a Excel | Pendiente |
| 15 | 23/11 | Mauricio Rodríguez | Dashboard con gráficos | Pendiente |
| 16 | 24/11 | José Luis Tincopa | Endurecimiento de seguridad y frontend servido por la API | Integrado en main mediante PR #22 |
| 17 | 25/11 | Huber Lope | Datos semilla y despliegue local | Pendiente |
| 18 | 26/11 | José Carlos Vitorino | QA de cierre y versión candidata | Pendiente |

## Desviaciones del plan

- Se conserva Identity Core como adaptador de cuentas (el plan pedía retirarlo en el Día 03).
- Compra tiene costo USD por línea y tipo de cambio propio; los campos históricos de Producto se conservan por compatibilidad.
- La concurrencia usa `xmin` en lugar de una columna de versión.
- Días 04–06 recuperados en la rama del Día 07, separados de los commits propios de Compras.


## Entrega de recepción e inventario

Responsable: José Luis Tincopa Paz.

Rama: `feat/2026-11-20-recepcion-inventario`.

Incluye recepción transaccional de compras, actualización del costo promedio
ponderado, entradas vinculadas a compras, protección contra duplicados y
concurrencia, consulta de stock y pantallas de recepción y alertas.

Suite comunicada en la entrega original del Día 08: 108 pruebas, 102 correctas,
0 fallidas y 6 omitidas. Durante la implementación también pasaron
las dos pruebas habilitadas de concurrencia PostgreSQL.

Detalle de uso, migraciones, permisos y comprobación manual:
[Día 08: recepción e inventario](DIA-08-RECEPCION-INVENTARIO.md).

La entrega original correspondía al Día 08. Los trabajos posteriores están descritos en [historial y anulación](VENTAS-HISTORIAL-ANULACION.md), [reportes](REPORTES-LINQ.md) y [seguridad](DIA-16-SEGURIDAD.md).

## Seguridad y frontend servido por la API

Aporte de José Luis Tincopa Paz (rama `feat/ventas-dia12`), integrado solo en su parte de seguridad: límites de intentos de login, errores API uniformes con `traceId` y Astro compilado servido por ASP.NET en un mismo puerto. El historial, la nota y la anulación ya estaban integrados en `main`, por eso no se duplicaron.
