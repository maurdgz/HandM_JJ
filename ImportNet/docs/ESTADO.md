# Estado

Actualizado: 27/11/2026. Referencia: plan de desarrollo v2 (periodo técnico 10–26/11/2026). Rama `feat/ventas-dia12`, integrada localmente con main hasta `cc501e3` (PR #20), incluyendo PR #19 y la mejora de Empleados del PR #8. PR #21 publicado; esta resolución todavía requiere push y revisión.

| Día | Fecha | Responsable | Módulo | Estado |
| --- | --- | --- | --- | --- |
| 01 | 07/11 | Mauricio Rodríguez | Empleados y autorización por rol | Hecho |
| 02 | 10/11 | José Carlos Vitorino | Arquitectura hexagonal y ajuste del modelo a tienda | Hecho (cerrado el 11/11) |
| 03 | 11/11 | Mauricio Rodríguez | Autenticación JWT + BCrypt y API REST pura | Hecho |
| 04 | 12/11 | José Luis Tincopa | Frontend base: Astro + Tailwind + shadcn | Base integrada; mejora de acceso y diálogo del PR #8 incorporada localmente a PR #21 |
| 05 | 13/11 | Huber Lope | Catálogo con búsqueda y paginación | Recuperado sobre el CRUD existente |
| 06 | 14/11 | José Carlos Vitorino | Proveedores y clientes | Recuperado sobre entidades y mapeos existentes |
| 07 | 15/11 | Mauricio Rodríguez | Compras de importación (borrador) y costo puesto en tienda | Implementado y probado; recepción fuera de alcance |
| 08 | 16/11 | José Luis Tincopa Paz | Recepción de compra e inventario | Integrado en main mediante PR #11 |
| 09 | 17/11 | Huber Lope | Ajustes de inventario y kardex | Integrado en main mediante PR #12 |
| 10 | 18/11 | José Carlos Vitorino | QA intermedio: compras, inventario y roles | Pendiente |
| 11 | 19/11 | Mauricio Rodríguez | Punto de venta (POS) | Integrado en main mediante PR #9 |
| 12 | 20/11 | José Luis Tincopa | Historial, nota de venta y anulación | Integrado en main por PR #19; pruebas y mejoras adicionales en PR #21 |
| 13 | 21/11 | Huber Lope | Reportes con LINQ | Integrado en main por PR #20; regresión adicional con anulación en PR #21 |
| 14 | 22/11 | José Carlos Vitorino | Pantallas de reportes y exportación a Excel | Pendiente |
| 15 | 23/11 | Mauricio Rodríguez | Dashboard con gráficos | Pendiente |
| 16 | 24/11 | José Luis Tincopa | Endurecimiento de seguridad y frontend servido por la API | Implementado y probado; PR #21 abierto, pendiente de publicar esta integración |
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

La entrega original correspondía al Día 08. Los trabajos posteriores están descritos en [Día 12](DIA-12-VENTAS.md) y [Día 16](DIA-16-SEGURIDAD.md).

## Entrega local de historial y seguridad

Responsable: José Luis Tincopa Paz. Rama: `feat/ventas-dia12`.

Incluye historial por propietario y fechas peruanas, nota imprimible, anulación transaccional con reposición única, límites de login, errores API uniformes y Astro compilado servido por ASP.NET. Conserva el POS, recepción, stock y kardex integrados por el equipo.

Verificación local del 27/11 tras integrar reportes: regresión de 154 pruebas aprobadas, 0 fallidas y 4 omitidas, con concurrencia real de anulación y migraciones en un esquema PostgreSQL temporal. Dos recorridos Chrome (ventas y Empleados) aprobados por separado. Astro check y build correctos; publish incluye wwwroot y dotnet run sirve frontend y API en un mismo puerto.

PR #21 está abierto. Se resolvieron localmente los conflictos con PR #19, conservando sus contratos paginados y pantallas, junto a la reposición transaccional y pruebas adicionales. Se incorporaron también los cambios del PR #8 y el nuevo main del PR #20; se verifica que anular descuente los totales de reportes y que el Vendedor solo acceda a su resumen. El push de esta resolución y la ejecución remota de CI siguen pendientes. Los días sin evidencia de cierre en esta revisión conservan su estado anterior; no se afirma que nadie esté trabajando en ellos.
