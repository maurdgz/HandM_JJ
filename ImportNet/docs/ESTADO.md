# Estado real — jornada 06/11/2026 (America/Lima)

## Implementado

- Cuatro capas .NET 10 y proyecto separado de pruebas; solución original conservada.
- Entidades centrales, FK restrictivas, índices y restricciones; migración InitialCreate generada y SQL revisable.
- Categorías y productos: crear/listar/obtener/editar/eliminar con validación y restricciones históricas.
- API con MediatR, códigos HTTP y ProblemDetails; login/logout Identity, políticas por rol y antiforgery.
- Bootstrap de administrador explícito mediante secretos, con Empleado asociado y tres roles.
- Layout y navegación por rol, login, inicio y listados/formularios MVC; Bootstrap local con licencia.

## Evidencia

- Restore y build ejecutados con SDK 10.0.400. Build: 0 errores, 0 advertencias tras actualizar Swagger por advertencia de seguridad de una dependencia anterior.
- 17 pruebas correctas: handlers de catálogo, restricciones relacionales, magnitudes, transiciones, inmutabilidad, arranque HTTP/Swagger, login/logout, CRUD API, guardado MVC, permisos y antiforgery.
- Se detectó y corrigió un fallo de duplicados Unicode en la primera ejecución. Se repitió la suite satisfactoriamente.
- Servicio local PostgreSQL 18 en ejecución. Sin ConnectionStrings__ImportNet ni secretos existentes del nuevo host. No se inspeccionó ni modificó el contenido de ImportNet y no se aplicó la migración.
- GitHub autentica como maurdgz con permiso de push. El propietario corrigió explícitamente el correo a mauricio.rodriguez@tecsup.edu.pe; se configura solo en este repositorio antes del primer commit, sin modificar la identidad global.

## Pendiente

- Aplicar migración y verificar bootstrap, login y CRUD sobre PostgreSQL con secretos locales; inspección visual en Rider/navegador.
- Administración de empleados; módulos de proveedores, clientes y almacenes/inventario.
- Ventas transaccionales: cálculo de totales servidor, identidad de empleado, descuento atómico, idempotencia, anulación con devolución única y pruebas de concurrencia reales en PostgreSQL. El modelo y las transiciones no equivalen a este módulo terminado.
- Reportes/dashboard con métricas reales y exportación Excel/PDF.
- No se implementaron pagos, SUNAT, aduanas, tracking, app móvil ni tienda pública.

## Próximo bloque del 07/11

Configurar secretos localmente, comprobar que ImportNet no contiene un esquema incompatible y aplicar InitialCreate; ejecutar bootstrap y prueba manual. Después implementar administración de empleados y entradas/ajustes de inventario con movimientos auditables, antes de habilitar órdenes de venta.
