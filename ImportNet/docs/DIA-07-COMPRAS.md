# Día 07 Compras de importación

Domingo 15/11/2026. Responsable: Mauricio Rodríguez Herrera. Entregable: compras en borrador y costo puesto en tienda en PEN. Rama: `feat/dia-07-compras-importacion`.

## Base y recuperación

SHA de partida y main actualizado: `512146923f26bf0571e370a1998a9fab18689932`. Fin de recuperación: `bd8b1520644aa78fafca6da75297ce295df91ff4`. Los commits posteriores de Compras se cuentan desde ese segundo SHA, excluyendo la recuperación y los commits importados.

- Día 04: merge `84672e3` de `origin/frontend-base`. Conserva los cinco commits `4726e86`, `9ea7caf`, `cfaa0c2`, `93978d8` y `6df855b` de José Luis. Se reutilizan Astro, React, Tailwind, shadcn, `AppLayout`, `AppShell`, `api.ts`, JWT en sessionStorage y logout ante 401. La API de empleados bloquea al Vendedor con 403 (`AuthApiTests`, `RecuperacionTests`). No se creó otro frontend.
- Día 05: se conservan los CRUD y repositorios de productos/categorías. `57d00fc` añade `/api/productos/buscar` con texto, categoría, stock bajo, Count y Skip/Take en Infrastructure; orden por nombre e ID. Se conserva el listado anterior para compatibilidad. `bd8b152` añade las pantallas. `CatalogoTests`, `SmokeTests` y `RecuperacionTests` verifican búsqueda «olla», SKU duplicado, referencias y omisión de costos al Vendedor.
- Día 06: se reutilizan `Proveedor`, `ProveedorProducto`, `Cliente` y sus tablas. `41604f4` añade puerto, repositorio y casos de uso; `bd8b152` añade pantallas. Documentos: DNI 8 dígitos, RUC 11, índice único. El Administrador gestiona terceros; Vendedor registra y busca clientes, pero no modifica clientes existentes ni gestiona proveedores. La migración `TercerosLocales` inserta «Cliente varios» por documento si no existe, sin asumir ID libre; el registro reservado no se puede editar ni eliminar.

Se revisaron `7aebed6` y `efdf6b5`: ventas y precios de venta continúan en PEN. `CostoImportacionUsd` y `TipoCambioImportacion` del catálogo se conservan como referencias históricas. Compras no escribe esos campos.

Instrucciones leídas: `frontend/AGENTS.md`, `DECISIONES.md`, `PRUEBA-MANUAL.md`, README y ESTADO. No existe un plan v2 completo ni una guía de ejecución adicional en el árbol o las ramas remotas revisadas. El DOCX encontrado en Descargas es el plan antiguo (MVC, exportación y calendario anterior); no se aplican sus requisitos incompatibles. El encargo actual y las decisiones vigentes definen el alcance. GitHub mostraba únicamente el PR #3 fusionado antes de abrir el PR de entrega.

## Uso y API

Iniciar API y frontend según README. En `/compras`, un Administrador pulsa «Nueva compra», selecciona proveedor y productos, indica fecha, cantidades, costos unitarios USD, tipo de cambio PEN/USD, flete PEN y aduana/agente PEN. La vista previa es estimada; guardar devuelve y muestra los importes confirmados por el servidor. Una compra `Recibida` se consulta sin acciones de edición o eliminación.

Todo `/api/compras` requiere la política `Administracion`:

- `GET /api/compras?pagina=1&tamano=20`: `{ items, count, paginaActual, tamano }`, orden descendente por fecha e ID.
- `GET /api/compras/{id}`: cabecera y costos por línea.
- `POST /api/compras`: 201, Location y detalle calculado.
- `PUT /api/compras/{id}`: 200 y detalle recalculado.
- `DELETE /api/compras/{id}`: 204 solo para Borrador.

POST y PUT reciben únicamente datos de entrada, sin estado ni totales:

```json
{
  "proveedorId": 1,
  "fecha": "2026-11-15T00:00:00Z",
  "tipoCambio": 3.8,
  "fletePen": 10,
  "aduanaPen": 5,
  "lineas": [
    { "productoId": 1, "cantidad": 2, "costoUnitarioUsd": 10 }
  ]
}
```

Resultado del ejemplo: subtotal USD 20, mercadería PEN 76, costo puesto PEN 91. Los IDs del ejemplo deben existir y estar activos. Estado y totales adicionales enviados por un cliente no se utilizan. Errores ProblemDetails: 400 validación, 401 sin sesión, 403 Vendedor, 404 inexistente, 409 recibida o conflicto de concurrencia/referencias.

Validaciones: fecha UTC; tipo de cambio positivo, máximo 1000 y 6 decimales; 1–100 líneas sin productos repetidos; cantidad positiva hasta un millón y 3 decimales; costo USD positivo hasta un millón y 4 decimales; gastos no negativos hasta un billón y 2 decimales. Se rechaza un total que exceda `numeric(18,2)`.

## Cálculo y persistencia

`CalculadoraCostoImportacion` es C# puro, usa decimal y no depende de EF ni HTTP. Base de cada línea: cantidad por costo unitario USD. La mercadería total convertida se redondea a dos decimales con `MidpointRounding.AwayFromZero`. Mercadería convertida, flete y aduana se reparten separadamente por valor USD: piso en centavos y asignación del residuo por mayor resto, con desempate por orden de entrada. Cada suma cierra exactamente, incluso cuando tres líneas dejan un centavo residual.

`Compra.Editar` valida y calcula antes de cambiar el agregado. Cabecera y líneas se guardan mediante una sola llamada a UnitOfWork/SaveChanges; EF aplica la operación atómicamente. Se usa xmin de PostgreSQL y se fuerza la actualización de cabecera cuando solo cambian líneas, para detectar carreras con una futura recepción. Las consultas EF residen en `CompraRepository`.

Migración `20261115175315_ComprasBorrador`: crea únicamente Compra, DetalleCompra e índices/FK; no altera ventas ni borra datos al aplicar Up. Se revisó y aplicó a PostgreSQL ImportNet existente junto con `TercerosLocales`, sin reset. Down de Compras elimina sus tablas y no se ejecutó.

## Contrato para recepción del 16/11

José Luis debe consumir los valores persistidos de cada `DetalleCompra`: `ProductoId`, `Cantidad`, `CostoUnitarioUsd`, `MercaderiaPen`, `FletePen` y `AduanaPen`. `Orden` conserva el desempate del prorrateo. `TotalPen` de la línea es la suma de los tres componentes PEN; `CostoUnitarioPuestoPen` se deriva como `TotalPen / Cantidad` sin redondeo intermedio. No multiplicar un unitario mostrado a dos decimales para reconstruir totales.

La recepción deberá diseñar su propia transición atómica Borrador→Recibida, control de concurrencia e idempotencia antes de registrar entradas, movimientos o costo promedio. Este avance no contiene esa transición ni pagos, kardex o entradas de stock. Crear, editar y eliminar borradores no modifica inventario ni costo promedio. No usar campos de ventas o cobros para costos USD.

## Verificación

Pruebas esenciales en `CostosImportacionTests`, `ComprasTests`, `ComprasApiTests` y `ComprasPostgreSqlTests`: proporción, centavos, cantidades/tipo de cambio inválidos, referencias, CRUD, 401/403 en todos los endpoints, estado/totales manipulados, Recibida bloqueada para editar y eliminar, stock y costo promedio intactos. PostgreSQL se prueba con rollback.

```powershell
dotnet build ImportNet.sln --configuration Release
dotnet test ImportNet.sln --no-build --configuration Release
$env:IMPORTNET_POSTGRES_TESTS = '1'
dotnet test ImportNet.sln --filter 'Database=PostgreSQL'
cd ImportNet/frontend
npm ci
npm run check
npm run build
```

Verificación final: build Release aprobado sin warnings ni errores; suite ordinaria con 49 pruebas correctas y 5 opcionales omitidas. Las 4 pruebas habilitadas de PostgreSQL pasaron con rollback. Frontend check/build aprobados sin errores ni warnings, con 10 avisos de deprecación.

Chrome: Swagger login/Authorize/catálogo aprobado; Astro login→crear→editar→consultar recibida→eliminar aprobado, con comprobación de Vendedor bloqueado y limpieza de sesión ante 401. El fixture verifica también que stock, costo promedio y movimientos permanecen intactos. El caso GET→PUT detectó la pérdida de Kind UTC en SQLite; `acdb87c` normaliza la fecha del DTO a UTC y añade su regresión. No cambia autenticación ni ventas.

La prueba opcional de Astro usa API Kestrel y SQLite temporales, usuarios ficticios y `astro dev --background`, que se detiene al finalizar. Requiere Chrome local y Python Playwright. Las credenciales viajan por stdin; no se guardan tokens ni capturas.

```powershell
$env:IMPORTNET_BROWSER_TESTS = '1'
dotnet test ImportNet.sln --no-build --configuration Release --filter 'Browser=Chrome'
```

En este equipo, Windows Application Control bloqueó DLL generadas por un compilador persistente iniciado en el sandbox. Se cerraron los build servers y se compiló Release con procesos nuevos y permiso de ejecución externo; las pruebas posteriores pasaron. No se modificó la política de Windows.

## Commits progresivos del Día 07

Los 15 hitos originales se registraron al terminar cada bloque, antes de comenzar el siguiente; no se dividió el trabajo al final:

1. `2ae87f4` cabecera y líneas.
2. `ad6de72` validaciones y borrador.
3. `411215b` cálculo PEN.
4. `3d030a3` prorrateo y centavos.
5. `27f4f8d` mapeo y migración.
6. `2242e48` consultas por repositorio.
7. `3053dee` creación atómica.
8. `412e3e9` edición y recálculo.
9. `eb9b93f` eliminación de borradores.
10. `9932712` API Administrador.
11. `370e63a` navegación y listado.
12. `01b530e` formulario y líneas.
13. `b62ca82` resumen y guardado.
14. `61cc693` permisos, inmutabilidad y stock.
15. `4351791` guía y contrato de recepción.
16. `acdb87c` fecha UTC y prueba Astro Chrome.

El commit posterior de cierre de evidencias completa 17 commits propios de Compras. Los commits de recuperación, el merge de frontend y la prueba complementaria de recuperación se registran aparte y no se suman a esos 17.

PR de entrega: https://github.com/maurdgz/HandM_JJ/pull/6. Se revisa CI .NET y Frontend en el SHA final antes de fusionar mediante merge commit a main, autorizado expresamente por el responsable durante la ejecución. No hacer squash ni reescribir los commits del equipo.
