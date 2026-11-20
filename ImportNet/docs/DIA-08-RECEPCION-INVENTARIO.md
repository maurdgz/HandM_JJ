# Día 08: recepción de compras e inventario

Responsable: José Luis Tincopa Paz.
Fecha de implementación: 20/11/2026.
Fecha prevista en el plan: 16/11/2026.
Rama: `feat/2026-11-20-recepcion-inventario`.

## Alcance

Se implementa la recepción de compras de importación y la consulta de
existencias del almacén principal.

- Transición de compra de Borrador a Recibida.
- Una entrada de inventario por línea recibida.
- Actualización del costo promedio ponderado en PEN.
- Registro del empleado autenticado que recibe la compra.
- Protección frente a recepción duplicada y operaciones concurrentes.
- Consulta de stock con búsqueda, paginación y alertas.
- Recepción desde la pantalla de Compras.
- Pantalla Stock y alertas para Administrador y Vendedor.

El módulo no incluye ajustes manuales de inventario, kardex, ventas,
anulaciones, reportes ni despliegue.

## Reglas de recepción

Guardar, editar o eliminar un borrador no modifica el inventario.

Solo un Administrador con empleado activo puede recibir una compra.
El empleado responsable se obtiene de la sesión autenticada.

La recepción se ejecuta dentro de una transacción:

1. Comprueba permisos y empleado activo.
2. Obtiene la compra y exige estado Borrador.
3. Comprueba que no tenga entradas de recepción previas.
4. Procesa las líneas ordenadas por ProductoId.
5. Obtiene o crea el inventario del almacén principal.
6. Actualiza el costo promedio y la cantidad disponible.
7. Registra una Entrada con CompraId, ProveedorId y EmpleadoId.
8. Cambia la compra a Recibida.
9. Guarda y confirma la transacción.

Si alguna operación falla, no deben persistir cambios parciales.

Una compra recibida solo permite consulta. No se puede recibir nuevamente,
editar ni eliminar.

## Costo promedio

Se utilizan los importes persistidos de cada DetalleCompra:

TotalPen = MercaderiaPen + FletePen + AduanaPen

Nuevo costo promedio =
(stock anterior × costo promedio anterior + TotalPen de la línea)
÷ (stock anterior + cantidad recibida)

El costo promedio se redondea a cuatro decimales con
MidpointRounding.AwayFromZero.

No se reconstruye el costo total multiplicando un costo unitario
redondeado para mostrarlo en pantalla.

Ejemplo:

- Stock anterior: 7.5 unidades.
- Costo promedio anterior: S/ 20.
- Entrada: 2 unidades.
- Costo total de entrada: S/ 80.
- Stock resultante: 9.5 unidades.
- Costo promedio resultante: S/ 24.2105.

La recepción conserva PrecioVenta, CostoImportacionUsd y
TipoCambioImportacion del producto.

## Persistencia y concurrencia

Se utiliza Almacen.PrincipalId como almacén de recepción.

La unidad de trabajo comparte un mismo AppDbContext entre los repositorios.
La recepción utiliza aislamiento Serializable.

En PostgreSQL se conserva el control optimista mediante xmin.
Los conflictos reconocidos de concurrencia se traducen a ConflictoException.

Los conflictos no provocan una repetición automática de la recepción.
El cliente debe consultar el estado y las existencias antes de reintentar.

MovimientoInventario incorpora CompraId nullable para conservar la
compatibilidad con otros tipos de movimientos.

El índice único filtrado
UX_Movimiento_EntradaCompraProductoAlmacen
protege la combinación CompraId, ProductoId y AlmacenId cuando
CompraId no es nulo y Tipo corresponde a Entrada.

Migraciones añadidas:

- 20261120062556_VincularMovimientosCompra
- 20261120072407_ProtegerEntradasCompra

Para aplicar las migraciones, desde la carpeta ImportNet:

```bat
dotnet tool restore --tool-manifest .\dotnet-tools.json
dotnet ef database update --project ImportNet.Infrastructure --startup-project ImportNet.API
```

Los scripts SQL se generan cuando se necesitan; no se versionan en este
entregable.

## API y permisos

| Operación | Administrador | Vendedor | Sin sesión |
| --- | --- | --- | --- |
| POST /api/compras/{id}/recibir | Permitido | 403 | 401 |
| GET /api/inventario/stock | Permitido | Permitido | 401 |

### Recepción

POST /api/compras/{id}/recibir

No requiere cuerpo. La compra, cantidades, costos y empleado responsable
se obtienen del servidor.

Respuestas principales:

- 200: compra recibida y detalle actualizado.
- 400: identificador o regla de negocio inválida.
- 401: sesión ausente o inválida.
- 403: acceso no permitido.
- 404: compra o referencia inexistente.
- 409: compra recibida, entradas existentes o conflicto de concurrencia.

### Consulta de stock

GET /api/inventario/stock?texto=olla&stockBajo=false&pagina=1&tamano=20

Parámetros:

- texto: búsqueda por nombre o código, hasta 100 caracteres.
- stockBajo: limita el resultado a cantidad menor o igual al mínimo.
- pagina: entre 1 y 1000000.
- tamano: entre 1 y 100.

Respuesta paginada:

```json
{
  "items": [
    {
      "productoId": 1,
      "codigo": "OL-01",
      "nombre": "Olla",
      "unidadMedida": "Unidad",
      "almacenId": 1,
      "cantidad": 9.5,
      "stockMinimo": 5,
      "activo": true,
      "stockBajo": false
    }
  ],
  "count": 1,
  "paginaActual": 1,
  "tamano": 20
}
```

Los valores del ejemplo son ilustrativos.

La respuesta de stock no contiene costos ni tipos de cambio para ninguno
de los dos roles.

Los productos sin inventario aparecen con cantidad cero. Los productos
inactivos permanecen visibles con su estado para conservar la consulta
de sus existencias.

## Interfaz

### Compras

Ruta: /compras. Acceso de Administrador.

- El borrador presenta la acción Recibir.
- Antes de enviar se solicita confirmación.
- Durante la operación se bloquean las acciones.
- Una recepción correcta actualiza el listado y el detalle.
- Una compra recibida deja de mostrar Editar, Eliminar y Recibir.
- Ante un conflicto o un resultado incierto se intenta consultar el
  estado antes de permitir que el usuario decida cómo continuar.

### Stock y alertas

Ruta: /stock. Acceso de Administrador y Vendedor.

- Búsqueda por nombre o código.
- Filtro de stock bajo.
- Tamaños de página 10, 20, 50 y 100.
- Cantidades con hasta tres decimales.
- Alertas textuales y visuales.
- Indicador de producto activo o inactivo.
- Estados de carga, error y consulta sin resultados.
- Acción Actualizar para consultar nuevamente las existencias.

## Pruebas automatizadas

Cobertura añadida:

- RecepcionDominioTests.
- RepositoriosInventarioTests.
- RecepcionCompraTests.
- RecepcionProteccionesTests.
- RecepcionConcurrenciaPostgreSqlTests.
- RecepcionApiTests.
- StockConsultaTests.
- StockApiTests.
- RecepcionStockIntegracionTests.

La integración comprueba una recepción con dos productos, la actualización
del stock consultado por Vendedor y el cambio de las alertas.

También provoca un fallo de persistencia en la SQLite temporal y comprueba
que la compra, costos, inventarios y movimientos no queden parcialmente
actualizados. Después retira el fallo simulado y verifica el reintento.

Última ejecución ordinaria comunicada el 20/11/2026:

- Total: 108.
- Correctas: 102.
- Fallidas: 0.
- Omitidas: 6.

Las dos pruebas de concurrencia habilitadas contra PostgreSQL también
se ejecutaron correctamente durante la implementación. Utilizan un esquema
temporal propio y lo eliminan al terminar.

Desde la raíz del repositorio:

```bat
dotnet build ImportNet.sln
dotnet test ImportNet.sln --no-build
```

Para ejecutar las pruebas de concurrencia PostgreSQL desde CMD:

```bat
set IMPORTNET_POSTGRES_TESTS=1
dotnet test ImportNet.sln --no-build --filter "FullyQualifiedName~RecepcionConcurrenciaPostgreSqlTests"
set IMPORTNET_POSTGRES_TESTS=
```

Para comprobar el frontend desde ImportNet/frontend:

```bat
npm run check
npm run build
```

Las pruebas de API y servicios no sustituyen la comprobación manual
de la interfaz.

## Comprobación manual

1. Iniciar la API y el frontend según el README.
2. Iniciar sesión como Administrador.
3. Consultar y anotar el stock de un producto activo.
4. Crear una compra de ese producto con cantidad 2.
5. Guardar el borrador y verificar que el stock no cambió.
6. Cancelar una confirmación de recepción y comprobar que sigue Borrador.
7. Confirmar la recepción y comprobar el estado Recibida.
8. Actualizar Stock y verificar que la cantidad aumentó exactamente en 2.
9. Recargar Compras y comprobar que la recepción se conserva.
10. Verificar que la compra recibida no permite editar, eliminar ni recibir.
11. Iniciar sesión como Vendedor y comprobar acceso a Stock y alertas.
12. Comprobar que el Vendedor no puede acceder a la gestión de Compras.

Las acciones de esta comprobación utilizan la base local configurada.

## Entrega

Este documento describe el trabajo de la rama de recepción e inventario.
La publicación de la rama y su integración a main se verifican por separado.

Se conservan los commits previos del equipo. No se incluyen credenciales,
tokens ni valores de User Secrets.