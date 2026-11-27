# Día 16 Seguridad y frontend en un solo host

Responsable: José Luis Tincopa Paz. Comprobación local: 27/11/2026.

## Cambios

- Login limitado por IP: 10 solicitudes por ventana fija de 60 segundos, sin cola. El exceso devuelve 429, ProblemDetails y Retry-After. Se cuentan también los accesos correctos; alcanzar el límite no revoca un token vigente. Configurable con `LoginRateLimit__PermitLimit` y `LoginRateLimit__WindowSeconds`.
- Se usa la IP de la conexión y no se confía en X-Forwarded-For enviado por el cliente. Si se coloca un proxy inverso, configurar explícitamente proxies confiables antes de habilitar forwarded headers; de otro modo sus clientes compartirán el límite de la IP del proxy.
- Errores API sin cuerpo, incluidos 401, 403, 404 y 405, con ProblemDetails y traceId. Se conservan los errores de validación del modelo y WWW-Authenticate. Los errores inesperados no muestran excepciones internas.
- API con Cache-Control no-store. Respuestas con nosniff, política de referrer y protección contra inclusión en frames.
- Revisión de validaciones existentes en empleados, catálogo, terceros, compras, ajustes y ventas. Se corrigió una línea de venta nula: devuelve 400 y no 500.
- JWT exige emisor, audiencia y clave Base64 de al menos 32 bytes, configurados mediante User Secrets en Development o variables de entorno. No se agregó ninguna clave real a los archivos versionados.
- DTOs y permisos: usuario sin hashes ni security stamp; catálogo y stock del Vendedor sin costos; historial y notas limitados al propietario; anulación y administración reservadas al Administrador. La autorización del backend no depende del menú ni de sessionStorage.
- Astro compilado se copia a wwwroot y ASP.NET sirve sus páginas y recursos. La API conserva /api, sin añadir CORS ni un fallback que convierta errores API en HTML.

## Ejecución en un solo host

Desde `ImportNet`, con Node 24+, SDK .NET 10, PostgreSQL migrado y los secretos locales configurados según el README:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./scripts/Preparar-Frontend.ps1
dotnet run --project ImportNet.API --launch-profile ImportNet
```

Abrir http://localhost:5080/login. Las páginas y /api usan ese mismo origen. No se necesita iniciar Astro dev. La sesión sigue en sessionStorage y se borra al cerrar la pestaña o al recibir 401.

El script ejecuta npm ci, check y build; luego copia dist a ImportNet.API/wwwroot. Para dependencias ya instaladas se puede usar `-SkipInstall`. Solo reemplaza wwwroot si contiene la marca del contenido generado por el propio script y rechaza enlaces. Un wwwroot manual sin marca requiere revisar sus archivos antes de reemplazarlo.

Repetir la preparación después de cambiar el frontend. wwwroot y dist están ignorados en Git; se versionan las fuentes y package-lock.json. HTML sin caché; recursos de /_astro con caché larga y nombres generados por el build.

Publicación local, después de preparar el frontend:

```powershell
dotnet publish ImportNet.API --configuration Release
```

Publish incluye wwwroot; no realiza un despliegue. Production mantiene HSTS y redirección HTTPS: configurar certificado, URL pública y AllowedHosts junto a `ConnectionStrings__ImportNet`, `Jwt__Issuer`, `Jwt__Audience` y `Jwt__SigningKey`. Swagger solo está disponible en Development.

## Comprobación

Desde la raíz del repositorio:

```powershell
$env:IMPORTNET_VENTAS_POSTGRES_TESTS = '1'
dotnet test ImportNet.sln --configuration Release --filter 'Browser!=Chrome'
Remove-Item Env:IMPORTNET_VENTAS_POSTGRES_TESTS
```

La concurrencia de ventas crea, migra y elimina un esquema temporal propio en PostgreSQL de loopback. Las cuatro pruebas antiguas que requieren IMPORTNET_POSTGRES_TESTS permanecen optativas. No se habilitan automáticamente contra las tablas reales del negocio.

Chrome opcional, con Chrome local, Python y Playwright instalados, después de preparar el frontend:

```powershell
$env:IMPORTNET_BROWSER_TESTS = '1'
dotnet test ImportNet.sln --configuration Release --filter 'FullyQualifiedName~VentasBrowserTests'
Remove-Item Env:IMPORTNET_BROWSER_TESTS
```

La prueba usa Kestrel, SQLite temporal y cuentas sintéticas, sin utilizar cuentas reales ni iniciar o detener servidores Astro del usuario. Comprueba redirección sin sesión, login, filtros por día peruano, datos de la nota, estilos de impresión, anulación, repetición 409, Vendedor sin anulación, venta ajena 404 y cierre por token inválido. Las credenciales de prueba se pasan a Python por stdin.

Evidencia local de esta entrega:

- Regresión sin navegador: 152 aprobadas, 0 fallidas, 4 omitidas; incluye concurrencia de anulación en PostgreSQL.
- Chrome de ventas: 1 aprobada. Seguridad HTTP comprobada con entradas nulas, incompletas y JSON inválido.
- Astro check: 0 errores, 0 warnings y 10 hints de deprecaciones existentes; build: 13 páginas. .NET conserva dos avisos xUnit2031 preexistentes en pruebas de recepción.
- Publish incluye inicio, login, historial, nota y recursos de Astro.
- dotnet run en un puerto temporal respondió 200 para páginas y 401 para /api/ventas anónimo; el proceso de comprobación se detuvo al terminar.
- Revisión de patrones y configuración en archivos versionados actuales: sin credenciales reales encontradas. La clave del contenedor PostgreSQL en CI es exclusivamente de prueba. Esta comprobación no equivale a auditar todo el historial Git.

CI prepara Astro, compila .NET, ejecuta la suite con PostgreSQL de prueba y verifica publish. La ejecución remota de CI queda pendiente del push y PR del equipo; esta entrega deja commits locales.

La nota puede imprimirse o guardarse como PDF desde el navegador; no se añadió un generador PDF en el servidor. Reportes, dashboard, seed de demostración y QA de cierre conservan su alcance y responsables en el plan.
