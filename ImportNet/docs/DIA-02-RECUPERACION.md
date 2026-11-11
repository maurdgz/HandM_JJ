# Recuperación Día 02 — ejecutada el 11/11/2026

Plan original: 10/11/2026, José Carlos Vitorino. Se mantiene la identidad Git real configurada y la fecha de ejecución; no se atribuye trabajo nuevo a otro integrante.

## Auditoría

GitHub y checkout limpio coincidían en `6a81905674ac1d026ca744f398e8c856454dd0a5`. Se inspeccionaron referencias de los cinco proyectos, entidades, repositorios, UnitOfWork, handlers, controladores, migraciones, pruebas e informes.

Se conservan `dbc779f` (dos roles), `7aebed6` (PEN y tienda local) y `6a81905` (puertos y adaptadores del catálogo). Domain no depende de EF ni ASP.NET; Application solo referencia Domain; Infrastructure referencia Application/Domain; API compone ambas. No hay AppDbContext, EF ni IQueryable en Domain/Application. El contrato especializado de administración de empleados conserva la transacción entre cuenta, roles y empleado, con bloqueo PostgreSQL para proteger al último administrador.

## Hitos: comprobar antes de modificar

| Hito | Resultado |
| --- | --- |
| 1. Auditoría | Documentada y diagnóstico de filas incorporado |
| 2–5. Puertos/adaptadores de categorías y productos | Ya implementados, conservados y verificados |
| 6–7. Puerto/adaptador de empleados | IEmpleadoRepository y EmpleadoRepository incorporados; DI y consumo de estado |
| 8. UnitOfWork/transacciones | Ya implementados; guardado atómico y conflictos, más transacción especializada de cuentas |
| 9–10. Handlers desacoplados | Ya implementados; sin consultas EF en Application |
| 11. Producto PEN | Ya implementado; completada referencia opcional USD/tipo de cambio detectada en revisión final |
| 12. Cliente/almacén | Modelo anterior conservado; migraciones protegidas y Principal aplicado |
| 13. Roles | Código anterior de dos roles conservado; fila legado sin asignaciones retirada durante transición auth |
| 14. Migraciones | SQL revisado, bloqueos de datos añadidos y AjusteModeloTienda aditiva |
| 15. CI/validación | Workflow restore/build/test y documentación |

No se crean repositorios para módulos futuros ni se fuerzan 15 commits sobre código ya implementado. Cinco commits de este bloque: 017085e, 5969844, 3ed18f5, 37b1a29 y efdf6b5. El último es una corrección del requisito de modelo detectada en la validación final, después de la implementación JWT; el orden real queda en Git.

## PostgreSQL y seguridad de datos

La inspección inicial encontró InitialCreate/EmployeeProfiles aplicadas, un usuario, un empleado y tablas Producto, Cliente, OrdenVenta, DetalleOrdenVenta, Almacen y MovimientoInventario vacías. Se revisó DIA-02-MIGRACIONES.sql y se aplicaron las cuatro migraciones anteriores. Las operaciones que eliminan estructuras solo continúan si no existen datos afectados; con filas detienen la migración para exigir un mapeo explícito. PrecioUsd no se convierte silenciosamente a PEN. El almacén protege identidad y secuencia.

AjusteModeloTienda agrega únicamente CostoImportacionUsd y TipoCambioImportacion opcionales con restricciones. Son referencia de costo USD y PEN/USD suministrada por el administrador; no convierten precios ni calculan promedios o inventario. Cliente local, ventas de mostrador y relaciones existentes se conservan. No se implementaron operaciones futuras de compras, recepción ni ventas.

## Evidencia

Antes de JWT: build sin advertencias, 28 pruebas ordinarias aprobadas y 2 PostgreSQL omitidas. Al habilitar PostgreSQL: 30 aprobadas y 1 navegador omitida. Las pruebas originales con cookies se ejecutaron fuera del sandbox Windows por DPAPI. La validación final API ya no depende de cookies: 32 pruebas aprobadas sin omisiones, con PostgreSQL/Chrome habilitados. Detalle final y tabla de commits en INFORME-DIAS-02-03.md.
