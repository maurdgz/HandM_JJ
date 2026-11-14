# Estado

Actualizado: 14/11/2026. Referencia: plan de desarrollo v2 (periodo técnico 10–26/11/2026). El detalle de cada día está en el plan; aquí solo se registra el avance.

| Día | Fecha | Responsable | Módulo | Estado |
| --- | --- | --- | --- | --- |
| 01 | 07/11 | Mauricio Rodríguez | Empleados y autorización por rol | Hecho |
| 02 | 10/11 | José Carlos Vitorino | Arquitectura hexagonal y ajuste del modelo a tienda | Hecho (cerrado el 11/11) |
| 03 | 11/11 | Mauricio Rodríguez | Autenticación JWT + BCrypt y API REST pura | Hecho |
| 04 | 12/11 | José Luis Tincopa | Frontend base: Astro + Tailwind + shadcn | Atrasado |
| 05 | 13/11 | Huber Lope | Catálogo con búsqueda y paginación | Atrasado |
| 06 | 14/11 | José Carlos Vitorino | Proveedores y clientes | Pendiente |
| 07 | 15/11 | Mauricio Rodríguez | Compras de importación (borrador) y costo puesto en tienda | Pendiente |
| 08 | 16/11 | José Luis Tincopa | Recepción de compra e inventario | Pendiente |
| 09 | 17/11 | Huber Lope | Ajustes de inventario y kardex | Pendiente |
| 10 | 18/11 | José Carlos Vitorino | QA intermedio: compras, inventario y roles | Pendiente |
| 11 | 19/11 | Mauricio Rodríguez | Punto de venta (POS) | Pendiente |
| 12 | 20/11 | José Luis Tincopa | Historial, nota de venta y anulación | Pendiente |
| 13 | 21/11 | Huber Lope | Reportes con LINQ | Pendiente |
| 14 | 22/11 | José Carlos Vitorino | Pantallas de reportes y exportación a Excel | Pendiente |
| 15 | 23/11 | Mauricio Rodríguez | Dashboard con gráficos | Pendiente |
| 16 | 24/11 | José Luis Tincopa | Endurecimiento de seguridad y frontend servido por la API | Pendiente |
| 17 | 25/11 | Huber Lope | Datos semilla y despliegue local | Pendiente |
| 18 | 26/11 | José Carlos Vitorino | QA de cierre y versión candidata | Pendiente |

## Desviaciones del plan

- Se conserva Identity Core como adaptador de cuentas (el plan pedía retirarlo en el Día 03).
- El costo USD y el tipo de cambio están en Producto; el plan los pone en Compra (Día 07).
- La concurrencia usa `xmin` en lugar de una columna de versión.
- Días 04 (frontend) y 05 (búsqueda de catálogo) atrasados.
