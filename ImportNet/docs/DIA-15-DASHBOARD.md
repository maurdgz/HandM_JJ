# Día 15 — Dashboard con gráficos shadcn

Responsable: Mauricio Rodríguez Herrera. Issue #16.
Base confirmada tras `git fetch origin --prune`: `bbe7ebb38cb517454bdefd4ceca96881ef6101fc`.
Rama: `feat/dia-15-dashboard`.

## Alcance confirmado en GitHub

La issue #16 pide tarjetas de ventas de hoy, ventas del mes, margen del mes y productos bajo mínimo para Administrador; gráficos shadcn/Recharts de ventas por mes, top productos y ventas por vendedor; selector de periodo; vista del Vendedor con sus ventas del día; estados de carga, vacío y error y adaptación a celular. Cada dato debe proceder de `/api/reportes/*`.

Se reutilizan los reportes LINQ integrados por PR #20. Los KPI de hoy/mes se mantienen referidos al calendario comercial de Perú; el selector modifica el periodo de los gráficos. PEN en todos los importes comerciales. El margen utiliza el costo promedio vigente, según el contrato aprobado del reporte existente.

El documento completo del plan v2 es externo según issue #2 y no está disponible en el repositorio ni enlazado en las issues consultadas. Se siguen la issue #16 y `DECISIONES.md`; no se recupera el plan anterior.

## Integración y límites

- Conservar AppShell, sidebar, estilos y autenticación existentes; sustituir únicamente el contenido de Inicio.
- Admin: `/api/reportes/dashboard` para KPI agregados; reportes existentes para gráficos.
- Vendedor: solo `/api/reportes/mi-resumen`, que obtiene el empleado del JWT.
- No implementar pantallas/exportaciones de la issue #15 ni datos semilla de la #18.
- Agregaciones en PostgreSQL; las pruebas SQLite pueden mantener una alternativa compatible.
- No cambiar reglas de ventas/anulación, monedas ni cuentas de compañeros.

## Verificación prevista

Comprobar sumas netas, exclusión de anuladas, límites UTC-5, margen vigente y acceso por rol. Validar PostgreSQL, POS transaccional y Astro desde navegador. Ejecutar restore, Release build/test, Astro check/build y CI. Registrar resultados finales aquí.
