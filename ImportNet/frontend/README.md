# Frontend ImportNet

Astro estático con TypeScript strict, Tailwind, React y shadcn/ui. Requiere Node 24+ y la API .NET configurada según el [README general](../../README.md).

## Un solo host

Desde la raíz del repositorio:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ImportNet/scripts/Preparar-Frontend.ps1
dotnet run --project ImportNet/ImportNet.API --launch-profile ImportNet
```

Abrir http://localhost:5080/login. El script instala con npm ci, comprueba tipos, compila Astro y copia dist a wwwroot de la API. Repetirlo al modificar el frontend; añadir `-SkipInstall` si las dependencias ya están instaladas. No se necesita un servidor Astro en este modo.

## Desarrollo con proxy

Desde esta carpeta, con la API ejecutándose:

```powershell
Copy-Item .env.example .env
npm.cmd ci
npm.cmd run astro -- dev --background
```

API_URL en .env apunta a la API local. Vite reenvía /api a esa dirección solo durante desarrollo. No colocar secretos en variables públicas ni en los archivos del frontend.

```powershell
npm.cmd run check
npm.cmd run build
npm.cmd run astro -- dev status
npm.cmd run astro -- dev logs
npm.cmd run astro -- dev stop
```

En Linux o macOS usar npm en lugar de npm.cmd. dist, .astro, node_modules, .env y wwwroot generado no se versionan.

## Rutas y sesión

- /login inicia sesión con JWT; token, vencimiento y roles se guardan en sessionStorage.
- /empleados solo para Administrador. El menú se adapta al rol; si el Vendedor fuerza la URL, la pantalla consulta la API y muestra Acceso denegado por su 403, conservando la sesión. El diálogo de edición permanece abierto mientras guarda.
- /ventas conserva el POS; /historial consulta por fechas y pagina; /nota?id=... permite imprimir la nota. /ventas/historial y /ventas/nota?id=... son accesos compatibles a esas mismas pantallas.
- Catálogos, compras, stock y kardex mantienen los permisos de sus endpoints.
- src/lib/api.ts adjunta Bearer y, al recibir 401, limpia la sesión y redirige a login. Un 403 muestra el error sin cerrar la sesión.

Detalles y pruebas: [Día 12](../docs/DIA-12-VENTAS.md) y [Día 16](../docs/DIA-16-SEGURIDAD.md).

La prueba opcional `FrontendBrowserTests` comprueba login, CRUD de empleados, bloqueo del diálogo durante guardado, estados de carga/error/vacío, 403 real del Vendedor, sesión por pestaña, logout y cierre por 401. Preparar wwwroot con el script antes de ejecutarla. Usa Kestrel con SQLite temporal y el build real; no inicia servidores Astro. Requiere Python con Playwright y Chrome local:

```powershell
$env:IMPORTNET_BROWSER_TESTS = '1'
dotnet test ../../ImportNet.sln --configuration Release --filter 'FullyQualifiedName~FrontendBrowserTests'
Remove-Item Env:IMPORTNET_BROWSER_TESTS
```

Se puede indicar otro Python mediante IMPORTNET_PYTHON y Chromium instalado con IMPORTNET_BROWSER_CHANNEL=chromium. Las credenciales sintéticas se pasan por stdin.
