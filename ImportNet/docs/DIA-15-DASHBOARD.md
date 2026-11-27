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


## Funcionamiento implementado

Inicio (`/`) mantiene el layout y la navegaci?n del equipo. Administrador recibe ventas netas de hoy, ventas y n?mero de operaciones del mes, ticket promedio del mes, margen del mes, productos bajo m?nimo y un resumen con productos activos/sin stock. Los KPI se calculan desde el inicio del mes hasta el final del d?a comercial actual.

Los gr?ficos muestran evoluci?n mensual, cinco productos m?s vendidos por cantidad y ventas por vendedor. El selector ofrece este mes y ?ltimos 3, 6 o 12 meses calendario, incluyendo el actual hasta hoy. Cambia los tres gr?ficos; no cambia los KPI rotulados como hoy/mes. Los meses sin filas se completan con cero despu?s de una respuesta real satisfactoria. Los importes son PEN y las ventas anuladas se excluyen.

El margen conserva la definici?n del reporte aprobado: ingresos netos menos cantidad por costo promedio **vigente**, con costo redondeado por producto. No representa costo hist?rico al momento de la venta.

Vendedor ve ?nicamente sus ventas del d?a, cantidad y ticket promedio; usa el empleado del JWT. No consulta KPI globales ni gr?ficos administrativos. El acceso se comprueba tambi?n en backend, no solamente ocultando la interfaz.

Card, Chart, Select y Skeleton provienen del registro shadcn/ui; los gr?ficos usan Recharts 3. Las tablas desplegables permiten leer valores exactos con teclado/lector de pantalla. Hay estados vac?os, carga, errores y reintento. Se cancelan respuestas obsoletas al cambiar periodo; las cifras anteriores no se presentan como actuales mientras se carga. Recharts se carga por separado ?nicamente al abrir Inicio como Administrador.

## Backend y endpoints

Nuevo: `GET /api/reportes/dashboard` (JWT, pol?tica Administracion). Controlador ? `ResumenDashboardQuery`/handler ? puerto `IDashboardRepository` ? adaptador EF Core. Tres consultas agregadas de ventas, margen e inventario comparten RepeatableRead en PostgreSQL.

Reutilizados:

- `GET /api/reportes/ventas-por-periodo?desde=AAAA-MM-DD&hasta=AAAA-MM-DD&agrupacion=Mes`
- `GET /api/reportes/top-productos?desde=AAAA-MM-DD&hasta=AAAA-MM-DD&top=5`
- `GET /api/reportes/ventas-por-vendedor?desde=AAAA-MM-DD&hasta=AAAA-MM-DD`
- `GET /api/reportes/mi-resumen` para Vendedor.
- `GET /api/clientes/por-documento/{documento}` para corregir selecci?n de cliente en POS.

Los reportes mantienen sus contratos y la alternativa SQLite existente. Para PostgreSQL, agrupaci?n, suma, conteo, orden y l?mite se ejecutan antes de materializar el resultado. El costo se redondea por producto tras agregar, conservando las reglas previas. No se implementa la pantalla de reportes/exportaci?n (#15).

## Revisi?n de los aportes anteriores

- **D?a 01:** empleados/perfiles/Administrador/Vendedor conservados. Pasan las pruebas de CRUD, bajas, roles y autorizaci?n.
- **D?a 03:** JWT, API REST, BCrypt y transici?n desde hashes Identity conservados y probados. La base local contiene una cuenta heredada: su hash cambia a BCrypt cuando hace un login v?lido, seg?n el mecanismo existente. No se cambi? su contrase?a ni se publicaron secretos.
- **D?a 07:** compras/costos y bloqueo de compras recibidas conservados; regresi?n API, costos y PostgreSQL correctos. La prueba de navegador se adapta al texto ampliado de recepci?n del equipo y usa PostgreSQL aislado, manteniendo las comprobaciones de importes, solo lectura y permisos.
- **D?a 11:** se conserva la transacci?n y protecci?n xmin existentes. Se corrige ?nicamente el selector de clientes del POS: b?squeda por DNI/RUC, Cliente varios y bloqueo mientras un documento queda sin resolver. PostgreSQL verifica carrera sincronizada por ?ltima unidad (una venta y un 409) y falta de stock en la segunda l?nea sin ventas/detalles/movimientos parciales. Chrome verifica venta real desde Astro y anulaci?n con reposici?n.

## Base de datos y conservaci?n

No se agreg? una migraci?n propia. Se revis? el SQL y se aplicaron las cuatro migraciones del equipo pendientes en esta instalaci?n: VincularMovimientosCompra, ProtegerEntradasCompra, AjustesInventarioKardex e IndicesReportes. Inspecci?n final: 14 aplicadas y ninguna pendiente. Se conservaron los datos comerciales (0 productos, 0 ventas, 1 cliente, 1 empleado y 1 cuenta en esta instalaci?n).

Las pruebas nuevas crean un esquema `dashboard_test_<GUID>` con migraciones reales, cuentas y operaciones de prueba; al terminar eliminan exclusivamente ese esquema generado. No modifican los registros de `public`. Las pruebas PostgreSQL previas mantienen sus transacciones con rollback. La interfaz de la aplicaci?n no utiliza datos simulados; una base sin ventas muestra el estado vac?o.

## Ejecuci?n

Desde la ra?z del repositorio, con conexi?n y JWT configurados en User Secrets o variables de entorno:

```powershell
dotnet restore ImportNet.sln
dotnet ef database update --project ImportNet/ImportNet.Infrastructure --startup-project ImportNet/ImportNet.API
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ImportNet/scripts/Preparar-Frontend.ps1
# Si node_modules ya est? instalado, se puede usar -SkipInstall.
dotnet run --project ImportNet/ImportNet.API --launch-profile ImportNet
```

Aplicaci?n: http://localhost:5080/ ; login: http://localhost:5080/login/ ; Swagger: http://localhost:5080/swagger en Development. Usar una cuenta existente; no hay credenciales incrustadas.

Modo Astro de desarrollo, en otra terminal desde la ra?z, manteniendo la API en 5080:

```powershell
Set-Location ImportNet/frontend
npm ci
npm run dev -- --background
# Astro muestra la URL; /api se reenv?a a localhost:5080.
npm run dev -- stop
```

Consultar migraciones antes de aplicarlas en otras instalaciones; no ejecutar comandos de recreaci?n de base.

## Validaci?n reproducible

```powershell
# Ra?z
dotnet restore ImportNet.sln
dotnet build ImportNet.sln --configuration Release --no-restore
dotnet test ImportNet.sln --configuration Release --no-build --no-restore
$env:IMPORTNET_POSTGRES_TESTS = '1'
dotnet test ImportNet.sln -c Release --no-build --filter 'Database=PostgreSQL'
# Optativas: Chrome instalado y python -m pip install playwright.
# Primero preparar frontend para verificar tambi?n un solo host.
$env:IMPORTNET_BROWSER_TESTS = '1'
dotnet test ImportNet.sln -c Release --no-build --filter 'FullyQualifiedName~DashboardBrowserTests'
dotnet test ImportNet.sln -c Release --no-build --filter 'FullyQualifiedName~ComprasBrowserTests|FullyQualifiedName~SmokeTests.Chrome'
```

Desde `ImportNet/frontend`: `npm run check` y `npm run build`.

El workflow .NET incorpora un servicio PostgreSQL ef?mero y ejecuta las pruebas nuevas de agregaci?n y concurrencia. No requiere secretos de la instalaci?n local. Los workflows existentes conservan check/build del frontend, compilaci?n/pruebas .NET y publicaci?n de API.

## Entrega

PR hacia main: https://github.com/maurdgz/HandM_JJ/pull/23 (Closes #16). No fusionar autom?ticamente. La tabla hist?rica de ESTADO se actualiza con las integraciones ya verificadas #19, #20 y #22.

El plan v2 completo no est? enlazado en el repositorio: esta es una limitaci?n documental, no se reemplaz? por el plan antiguo. La issue #15 y los datos semilla de #18 siguen fuera de este aporte. El margen sigue dependiendo del costo vigente por decisi?n del modelo existente.


## Resultados de cierre

- Restore correcto; build Release sin errores. Permanecen dos advertencias xUnit2031 preexistentes en las pruebas de recepci?n del compa?ero (40 y 43); no se alteraron esas pruebas.
- Suite general: 144 aprobadas, 11 optativas omitidas, 0 fallidas.
- PostgreSQL habilitado: 9 aprobadas. Las pruebas de navegador se habilitan por separado.
- Chrome: 2 recorridos del dashboard aprobados (Astro dev con proxy y frontend compilado servido por API), m?s Swagger y Compras aprobados individualmente. No se suprimieron pruebas existentes.
- Astro check: 0 errores, 0 warnings y 10 hints de deprecaciones existentes. Astro build: 13 p?ginas correctas; sin advertencia de bundle grande tras separar Recharts.
- Swagger documenta `/api/reportes/dashboard` y Bearer; an?nimo recibe 401 y Vendedor 403 en datos administrativos.
- En el escenario PostgreSQL calculado a mano: hoy 150 PEN/2 ventas; mes 180 PEN/3 ventas; ticket 60 PEN; margen 140 PEN. Coincide con reportes de d?as, meses, top y vendedores. Las anuladas y los registros fuera de los l?mites peruanos no entran.
- En navegador: venta POS con descuento confirma 95 PEN y margen 75 PEN; tras anularla vuelve a cero; otra venta de Vendedor muestra 50 PEN solo en su resumen. Se verifica reposici?n y rechazo por falta de stock.
- CI .NET, publicaci?n de API, Frontend y `dashboard-postgres`: correctos en `116722d` antes del cierre documental. Evidencia: https://github.com/maurdgz/HandM_JJ/actions/runs/45983856764 y https://github.com/maurdgz/HandM_JJ/actions/runs/45983856757 . Los checks del ?ltimo commit se consultan en el PR #23.
- Compatibilidad: `origin/main` se volvi? a consultar por fetch; conserva la base `bbe7ebb38cb517454bdefd4ceca96881ef6101fc`, ancestro de la rama. Sin merges ajenos, reset, force push ni cambios de identidad.

## Archivos creados o modificados

- `M` `.github/workflows/dotnet.yml`
- `M` `ImportNet/ImportNet.API/Controllers/Api/ReportesController.cs`
- `A` `ImportNet/ImportNet.Application/UseCases/Reportes/ResumenDashboardQuery.cs`
- `A` `ImportNet/ImportNet.Domain/Entities/ResumenDashboard.cs`
- `A` `ImportNet/ImportNet.Domain/Interfaces/IDashboardRepository.cs`
- `A` `ImportNet/ImportNet.Infrastructure/Repositories/DashboardRepository.cs`
- `A` `ImportNet/ImportNet.Infrastructure/Repositories/ReporteRepository.Postgres.cs`
- `M` `ImportNet/ImportNet.Infrastructure/Repositories/ReporteRepository.cs`
- `M` `ImportNet/ImportNet.Infrastructure/ServiceRegistrationExtensions.cs`
- `M` `ImportNet/ImportNet.Tests/ComprasBrowserTests.cs`
- `A` `ImportNet/ImportNet.Tests/DashboardBrowserTests.cs`
- `A` `ImportNet/ImportNet.Tests/DashboardPostgreSqlTests.cs`
- `A` `ImportNet/ImportNet.Tests/DashboardTests.cs`
- `A` `ImportNet/ImportNet.Tests/PosPostgreSqlTests.cs`
- `M` `ImportNet/ImportNet.Tests/SmokeTests.cs`
- `M` `ImportNet/ImportNet.Tests/check_compras_browser.py`
- `A` `ImportNet/ImportNet.Tests/check_dashboard_browser.py`
- `A` `ImportNet/docs/DIA-15-DASHBOARD.md`
- `M` `ImportNet/docs/ESTADO.md`
- `M` `ImportNet/docs/REPORTES-LINQ.md`
- `M` `ImportNet/frontend/package-lock.json`
- `M` `ImportNet/frontend/package.json`
- `M` `ImportNet/frontend/src/components/AppShell.tsx`
- `M` `ImportNet/frontend/src/components/PuntoVentaPage.tsx`
- `A` `ImportNet/frontend/src/components/SelectorClienteVenta.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/AdminDashboard.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/DashboardStates.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/IndicadoresDashboard.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/TopProductosChart.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/VendedorDashboard.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/VentasMesChart.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/VentasVendedorChart.tsx`
- `A` `ImportNet/frontend/src/components/dashboard/useDashboardResource.ts`
- `A` `ImportNet/frontend/src/components/ui/card.tsx`
- `A` `ImportNet/frontend/src/components/ui/chart.tsx`
- `A` `ImportNet/frontend/src/components/ui/select.tsx`
- `A` `ImportNet/frontend/src/components/ui/skeleton.tsx`
- `A` `ImportNet/frontend/src/lib/dashboard.ts`
- `M` `README.md`
- `A` `ImportNet/docs/CONTINUIDAD-DIA-15.md` (actualiza el bloqueo inicial ya resuelto).

## Commits progresivos

26 commits funcionales/de verificaci?n publicados antes del commit documental que agrega este cierre; 27 contando el cierre. Todos conservan la identidad Git existente. Para obtener los SHA completos y el commit del propio cierre sin autorreferencias:

```powershell
git log --reverse --format="%H %s" bbe7ebb38cb517454bdefd4ceca96881ef6101fc..feat/dia-15-dashboard
```

- `afb3715` docs(dashboard): definir alcance confirmado e integracion con reportes
- `7a818d3` feat(dashboard): definir contrato de indicadores en PEN
- `025be0c` feat(dashboard): agregar ventas margen e inventario en PostgreSQL
- `77a8f06` feat(api): exponer KPI con MediatR y autorizacion administrativa
- `9d08398` perf(reportes): agrupar ventas y vendedores en PostgreSQL
- `d06f425` perf(reportes): agregar top y margen por producto en servidor
- `a5d012c` test(dashboard): validar totales fechas anuladas y permisos por rol
- `0b7c21f` feat(ui): instalar Card Chart Select Skeleton y Recharts de shadcn
- `c2237d8` feat(frontend): integrar cliente tipado de reportes y periodos de Peru
- `1d5fc69` feat(dashboard): presentar tarjetas KPI y resumen de inventario
- `c7002b0` feat(dashboard): graficar evolucion mensual con Chart y Recharts
- `e32eacd` feat(dashboard): visualizar top productos y ventas por vendedor
- `b4e4af5` feat(dashboard): manejar carga errores reintentos y solicitudes obsoletas
- `44f5135` feat(dashboard): conectar vista administrativa y selector de periodo
- `3bd37c0` feat(dashboard): integrar Inicio por rol y resumen diario del vendedor
- `edc17a7` fix(pos): seleccionar clientes por documento y bloquear seleccion pendiente
- `c9bf9ec` fix(reportes): validar ranking y KPI con migraciones en PostgreSQL
- `5794286` test(pos): verificar ultima unidad y rollback multilinea en PostgreSQL
- `0fed358` test(dashboard): verificar Astro POS roles y vista movil en Chrome
- `911dc94` perf(frontend): cargar graficos solo en el dashboard administrativo
- `62e6faf` fix(ci): validar rutas Astro y mantener proteccion JWT en smoke test
- `6b7e703` fix(dashboard): mostrar punto de ventas en periodos de un solo mes
- `18bf019` fix(dashboard): mantener KPI consistentes durante ventas concurrentes
- `599f3f3` test(dashboard): comprobar frontend compilado servido por la API
- `116722d` ci(dashboard): verificar agregaciones y concurrencia con PostgreSQL real
- `36a4f1e` test(compras): validar flujo vigente y permisos en PostgreSQL desde Astro
