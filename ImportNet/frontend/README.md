# Frontend de ImportNet

Astro con TypeScript strict, Tailwind CSS, islas React y componentes shadcn/ui. Incluye login JWT y gestión de empleados conectada a la API .NET. Los componentes de Toast usan Sonner.

## Requisitos y arranque

Usar Node.js 24 y una API .NET configurada según el [README principal](../../README.md). Las credenciales de la API se mantienen en User Secrets o variables de entorno.

Desde `ImportNet/frontend`:

```bash
npm ci
cp .env.example .env
# En Windows PowerShell: Copy-Item .env.example .env
npm run dev -- --background
```

Configurar en `.env` la dirección del servidor .NET, sin el sufijo `/api`:

```dotenv
API_URL=http://localhost:5080
```

Abrir `http://localhost:4321/login`. Vite reenvía `/api` al servidor indicado por `API_URL`; el navegador usa rutas relativas y el JWT se envía en `Authorization: Bearer`.

```bash
npm run astro -- dev status
npm run astro -- dev logs
npm run astro -- dev stop
```

El proxy es para desarrollo. `npm run build` genera archivos estáticos en `dist`; al desplegarlos, el servidor debe encaminar `/api` a .NET en el mismo origen. `astro preview` no reemplaza esa configuración de producción.

## Sesión y permisos

- `/login` valida correo y contraseña con React Hook Form y Zod. Tras autenticar consulta `/api/auth/me` y guarda token, vencimiento, usuario y roles en `sessionStorage`.
- `src/lib/api.ts` adjunta el Bearer. Una respuesta 401 de una petición autenticada limpia las cuatro claves de sesión y redirige a `/login`. Un 401 de login muestra el error de credenciales en el formulario.
- `AppLayout.astro` monta `AppShell` como isla React. Sin token vigente, el guard redirige a `/login`. Cerrar sesión elimina las claves de la aplicación.
- El nombre real del rol administrador del backend es `Administrador` (el «Admin» del issue #1). No se introduce un segundo rol `Admin`.
- El vendedor no ve Empleados en el menú. Si entra directamente a `/empleados`, la UI consulta la API y muestra **Acceso denegado** ante el 403, conservando su sesión. El backend aplica la política `Administracion`; ocultar el enlace es solo una ayuda de navegación.
- La sesión pertenece a la pestaña. Otra pestaña abierta sin `opener` comienza sin sesión; algunos navegadores pueden copiar `sessionStorage` al duplicar una pestaña o restaurarla.

## Empleados

La tabla consulta `GET /api/empleados`. El formulario shadcn dentro de un Dialog crea con POST y edita con PUT. Mientras se guarda, los campos y botones quedan deshabilitados y el diálogo permanece abierto ante Escape, cierre o clic exterior, para evitar perder el resultado de una operación pendiente.

Eliminar requiere confirmación y llama a DELETE. El backend realiza una **baja lógica**: el empleado queda inactivo y conserva su historial. La pantalla informa cargas, errores con opción de reintentar, lista vacía y resultados mediante Toast.

## Verificación

```bash
npm run check
npm run build
```

El workflow existente `.github/workflows/frontend.yml` ejecuta ambos comandos después de `npm ci` en todos los pull requests, con Node.js 24. No requiere cuentas ni credenciales para compilar.

La prueba de navegador se ejecuta contra Astro, una API .NET Kestrel y SQLite en memoria. Crea cuentas temporales y no modifica PostgreSQL ni usa cuentas reales. Desde la raíz del repositorio, en macOS/Linux:

```bash
python3 -m venv .local-tools/browser
.local-tools/browser/bin/python -m pip install -r ImportNet/ImportNet.Tests/browser-requirements.txt
.local-tools/browser/bin/python -m playwright install chromium
IMPORTNET_BROWSER_TESTS=1 \
IMPORTNET_PYTHON="$PWD/.local-tools/browser/bin/python" \
dotnet test ImportNet.sln --configuration Release --filter FullyQualifiedName~FrontendBrowserTests
```

Se puede usar Chrome ya instalado con `IMPORTNET_BROWSER_CHANNEL=chrome`. En Windows, usar el ejecutable Python de `Scripts` del entorno y definir esas variables mediante `$env:` en PowerShell.

La prueba verifica login y validación, almacenamiento de sesión, Bearer mediante el proxy, creación/edición/baja con confirmación, bloqueo del diálogo mientras guarda, estados de carga/error/vacío, menú del vendedor, 403 real al entrar por URL, cierre de sesión, pestaña independiente, vencimiento y 401 real con token inválido. Solo se simulan respuestas para los estados visuales de carga/error/vacío; el CRUD y los permisos usan la API real. Las credenciales de prueba se pasan por stdin, sin archivos ni capturas.
