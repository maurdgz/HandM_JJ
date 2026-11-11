# Prueba manual — API JWT

Desde la carpeta ImportNet, configurar JWT con `scripts/Configurar-Jwt.ps1`, inspeccionar PostgreSQL y ejecutar el perfil ImportNet de ImportNet.API. Swagger está en http://localhost:5080/swagger.

1. Ejecutar POST /api/auth/login con credenciales locales. Una contraseña incorrecta o un empleado inactivo responde 401. La cuenta antigua se convierte a BCrypt solo tras una contraseña válida.
2. Copiar accessToken en Authorize (sin prefijo) y ejecutar GET /api/auth/me. No aparecen hashes ni sellos.
3. Como Administrador, crear/editar/consultar/eliminar categoría y producto sin historial. Duplicado y categoría referenciada producen 409; precio negativo, costo USD sin tipo de cambio o cambio cero producen 400. Precio de venta en PEN.
4. Consultar costoImportacionUsd y tipoCambioImportacion proporcionados por el administrador. No existe conversión automática del precio ni actualización del promedio.
5. Crear Vendedor desde POST /api/empleados. Obtener su JWT en otra sesión o reemplazando Authorize. Puede consultar catálogo; no modificarlo ni administrar empleados (403). La respuesta de productos omite todos los costos y el tipo de cambio.
6. Restablecer JWT del administrador y cambiar roles/desactivar al empleado temporal. Su JWT anterior responde 401 de inmediato. No se permite retirar el rol ni desactivar al último Administrador activo (409).
7. Sin Authorization, recursos protegidos responden 401. Token manipulado/vencido responde 401. Ya no existen vistas ni rutas de cuenta MVC.

Automatización: pruebas HTTP SQLite y PostgreSQL, además de Chrome usando POST /login, Authorize, GET /me, empleados y catálogo. Las pruebas PostgreSQL revierten sus registros. No probar contra cuentas reales cambiando sus accesos.
