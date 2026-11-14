# Prueba manual

Requisito: API levantada según el [README](../../README.md) y Swagger abierto en http://localhost:5080/swagger.

## Pasos

1. `POST /api/auth/login` con credenciales locales → 200 con `accessToken`. Contraseña incorrecta o empleado inactivo → 401.
2. Pegar el token en **Authorize** (sin prefijo) y ejecutar `GET /api/auth/me` → usuario, empleado y roles, sin hashes.
3. Como Administrador, crear, editar, consultar y eliminar una categoría y un producto.
4. Repetir un código o eliminar una categoría con productos → 409.
5. Enviar precio negativo, costo USD sin tipo de cambio o tipo de cambio cero → 400.
6. Crear un Vendedor con `POST /api/empleados` e iniciar sesión con él.
7. Como Vendedor, consultar catálogo → 200 sin costos ni tipo de cambio. Modificar catálogo o empleados → 403.
8. Como Administrador, desactivar o cambiar roles del Vendedor → su token anterior responde 401.
9. Intentar desactivar o quitar el rol al último Administrador activo → 409.
10. Llamar a un recurso protegido sin token, o con token vencido o manipulado → 401.

No cambiar accesos de cuentas reales durante la prueba; usar empleados temporales.

## Endpoints

| Método | Ruta | Permiso |
| --- | --- | --- |
| POST | `/api/auth/login` | Anónimo |
| GET | `/api/auth/me` | Autenticado |
| GET | `/api/categorias`, `/api/categorias/{id}` | Administrador, Vendedor |
| POST | `/api/categorias` | Administrador |
| PUT, DELETE | `/api/categorias/{id}` | Administrador |
| GET | `/api/productos`, `/api/productos/{id}` | Administrador, Vendedor |
| POST | `/api/productos` | Administrador |
| PUT, DELETE | `/api/productos/{id}` | Administrador |
| GET, POST | `/api/empleados` | Administrador |
| GET, PUT | `/api/empleados/{id}` | Administrador |
| PATCH | `/api/empleados/{id}/estado` | Administrador |
| PUT | `/api/empleados/{id}/roles` | Administrador |
| GET | `/api/empleados/roles` | Administrador |

Respuestas: crear 201 con `Location`; modificar y eliminar 204; validación 400; sin token 401; sin permiso 403; inexistente 404; conflicto 409. Los errores usan ProblemDetails.

Ejemplo de producto:

```json
{
  "codigo": "USB-001", "nombre": "Cable USB", "unidadMedida": "Unidad",
  "precioVenta": 12.50, "categoriaId": 1, "stockMinimo": 2,
  "costoImportacionUsd": 2.50, "tipoCambioImportacion": 3.85
}
```
