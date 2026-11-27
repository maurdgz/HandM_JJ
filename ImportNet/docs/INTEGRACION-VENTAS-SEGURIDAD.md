# Integración de ventas, Empleados y seguridad

Revisión del 27/11/2026 contra el plan v2 completo. Responsable de esta rama: José Luis Tincopa Paz.

## Base y resolución

- main cc501e3 incluye PR #19 (ventas) y PR #20 (reportes LINQ).
- Se incorpora el commit e50612a del PR #8 conservando su autor y su historial.
- Se mantiene la rama publicada feat/ventas-dia12 del PR #21. La actualización es mediante merges; no requiere force push.
- La rama local backup/ventas-dia12-antes-integracion-20261127 conserva el punto anterior a la resolución. Los stashes preexistentes permanecen guardados.

Los conflictos de PR #21 afectaban IVentaRepository, VentaRepository, AppShell, NotaVentaPage y estilos globales. Además, el merge automático duplicaba acciones HTTP, comandos y propiedades de UnitOfWork. Se eliminaron esas duplicaciones y los modelos y pantallas que habían quedado sustituidos.

Se respetan los contratos de main:

| Operación | Contrato final |
| --- | --- |
| Historial | Pagina de ResumenVenta: items, count, pagina, tamano |
| Nota | NotaVenta con cabecera plana y lineas; venta ajena al Vendedor devuelve 403 |
| Anulación | 200 con VentaAnuladaDto; repetición 409, sin reponer de nuevo |
| Rutas | /historial y /nota; /ventas/historial y /ventas/nota siguen accesibles como alias de las mismas pantallas |

La anulación conserva el bloqueo PostgreSQL por venta, la validación de las salidas originales, el rollback y las entradas referenciadas con empleado y motivo. Las pruebas comprueban el contrato 200, el stock y los reportes; no solo el código HTTP. Se normalizan las fechas devueltas a UTC también en SQLite.

Empleados consulta la API al acceder por URL: el Vendedor recibe 403 y ve Acceso denegado; el menú sigue oculto. El diálogo permanece abierto mientras guarda. La prueba de este módulo usa el build servido por la API y cuentas temporales.

Los reportes nuevos se probaron junto con anulación: venta registrada suma, venta anulada deja de sumar, caja informa anuladas aparte y el resumen del Vendedor no incluye ventas ajenas. Se corrigió validación de fechas extremas para evitar desbordamientos y errores 500.

## Verificación local

- 154 pruebas sin navegador aprobadas, 0 fallidas, 4 PostgreSQL antiguas optativas omitidas. Incluye concurrencia real de anulación con todas las migraciones en un esquema PostgreSQL temporal.
- 2 pruebas Chrome aprobadas: ventas y Empleados sobre build Astro + API Kestrel + SQLite aislado. Los escenarios antiguos de navegador de Swagger y compras no se ejecutaron.
- Astro check: 0 errores, 0 warnings y 10 hints existentes. Build: 15 páginas.
- Publish contiene login, Empleados, POS, historial, nota, alias y recursos compilados. .NET conserva dos avisos xUnit2031 existentes en pruebas de recepción.
- No se añadieron credenciales reales, firmas de herramientas ni coautores a los commits nuevos.

## Qué queda para integrar en GitHub

Desde la raíz del proyecto, el usuario publica la resolución:

```powershell
git push origin feat/ventas-dia12
```

El PR #21 se actualiza automáticamente. Revisar los checks .NET y Frontend y pedir revisión antes de fusionar. La mejora del PR #8 ya está contenida en la rama: no hace falta copiarla de nuevo; comprobar su estado después de integrar PR #21.

Este trabajo no envió cambios a GitHub ni fusionó PRs remotos. Hasta el push, GitHub sigue mostrando la versión anterior del PR #21.

## Migración del nuevo main

PR #20 incorpora 20261127035314_IndicesReportes: crea índices de Venta y Compra y reemplaza el índice simple de empleado por uno compuesto de empleado y fecha. Se verificó en PostgreSQL temporal; no se aplicó a las tablas reales del usuario.

Para actualizar una instalación local, revisar primero el SQL y aplicar desde ImportNet:

```powershell
dotnet ef migrations script --idempotent --project ImportNet.Infrastructure --startup-project ImportNet.API
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API
```

## Plan y pendientes del equipo

Las entregas de José Luis de Días 04, 08, 12 y 16 están cubiertas por la base integrada y esta rama, pendientes de revisión y fusión de PR #21. No se declara terminado todo el plan.

Reportes LINQ (Día 13) ya está en main. Pantallas de reportes y Excel (Día 14), dashboard (Día 15), seed de demostración (Día 17) y QA de cierre (Día 18) mantienen los responsables del plan y su cierre pendiente. La ventana de documentación, instalación limpia y ensayo del 27/11 al 04/12 sigue siendo trabajo del equipo. La permanencia de Identity como adaptador ya está registrada como desviación; esta integración no cambia la política de cuentas.
