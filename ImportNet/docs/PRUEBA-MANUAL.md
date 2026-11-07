# Prueba manual pendiente en PostgreSQL

1. Configurar conexión en user-secrets, revisar esquema existente y SQL InitialCreate, aplicar migración revisada. Ejecutar bootstrap explícito con secretos propios y retirar su contraseña del almacén.
2. Arrancar `dotnet run --project ImportNet.API`; abrir `/cuenta/login`. Credencial incorrecta muestra error genérico; la correcta lleva a Inicio. No introducir contraseñas en tickets o capturas.
3. En Categorías, crear Electrónica, editar nombre y comprobar listado. Un duplicado, incluso variando mayúsculas, debe mostrar error visible.
4. Crear producto USB-001 en la categoría: peso 0.125 kg, precio 4.50 USD, partida 854442 y stock mínimo 2. Debe aparecer en el listado. Editarlo y comprobar persistencia tras recargar.
5. Intentar peso cero, precio negativo y código duplicado: no deben guardarse. Intentar eliminar categoría con producto: debe mostrar conflicto. Eliminar producto sin historial y después categoría: ambos desaparecen.
6. En sesión autenticada, `GET /cuenta/csrf` devuelve token. Enviar POST API conservando cookies y encabezado X-CSRF-TOKEN: 201. Sin token: 400. GET por id inexistente: 404.
7. Cuando exista alta administrativa de empleados, crear Vendedor y Almacenero. Vendedor consulta pero no modifica (403 en API); Almacenero crea/edita catálogo. Un usuario sin sesión recibe 401 en API.
8. Cerrar sesión con el botón (POST protegido). El catálogo API vuelve a responder 401. Probar que ReturnUrl externo no redirige fuera del host.

Los flujos de login, autorización, antiforgery, API y parte de MVC ya se prueban automáticamente con SQLite en memoria. Estos pasos contra PostgreSQL y la inspección visual en navegador no se han ejecutado.
