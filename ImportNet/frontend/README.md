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
- /empleados solo para Administrador. El menú se adapta al rol; los permisos reales los impone la API.
- /ventas conserva el POS; /historial consulta por fechas; /nota?id=... permite imprimir la nota.
- Catálogos, compras, stock y kardex mantienen los permisos de sus endpoints.
- src/lib/api.ts adjunta Bearer y, al recibir 401, limpia la sesión y redirige a login. Un 403 muestra el error sin cerrar la sesión.

Detalles y pruebas: [historial y anulación](../docs/VENTAS-HISTORIAL-ANULACION.md) y [Día 16](../docs/DIA-16-SEGURIDAD.md).
