# Decisiones vigentes — 11/11/2026

- ASP.NET Core Web API .NET 10, sin MVC, cookies ni frontend. Swagger Bearer y JWT HS256 de dos horas.
- Identity Core permanece como adaptador de persistencia/validación de las cuentas previas; no como esquema HTTP. Usuario/Rol/UsuarioRol se proyectan desde las tablas existentes para conservar IDs, relaciones y hashes.
- BCrypt costo 12 con SHA-384 previo; transición desde Identity solo al verificar contraseña correcta. JWT consulta estado, roles y versión de acceso en cada petición. Costo, tipo de cambio y promedio solo visibles al Administrador.
- CostoImportacionUsd y TipoCambioImportacion son referencias opcionales del producto, sin conversión de precios ni lógica de compras. PrecioVenta y CostoPromedio se expresan en PEN. Codigo es el SKU.
- Migraciones existentes protegidas frente a datos que necesiten conversión/archivo. Las tablas afectadas estaban vacías antes de aplicar. AjusteModeloTienda es aditiva; RetirarRolAlmacenero rechaza asignaciones existentes. Tablas Identity conservadas.
- Se mantienen los puertos y handlers previos de catálogo y la administración transaccional de empleados. No se agregan abstracciones genéricas ni módulos de siguientes jornadas.
- Las decisiones que siguen son registro histórico de sus fechas originales. Sus referencias a MVC, tres roles o exportación ya no describen el comportamiento actual. Ver README e INFORME-DIAS-02-03.md.

---
# Decisiones — 06/11/2026

Actualización 07/11: empleados y roles usan un contrato específico para guardar cuenta y empleado atómicamente, autorización en los casos de uso y bloqueo transaccional PostgreSQL para conservar al último Administrador activo. Estado activo reside en Empleado; correo/teléfono y credencial residen en Identity. Nuevos perfiles obligatorios, perfiles antiguos conservados sin inventar documentos. Ver `DIA-01-EMPLEADOS.md`.

- Se usa exclusivamente .NET 10 por instrucción explícita del propietario, que reemplaza el texto académico previo de .NET 8. La alineación del documento académico con .NET 10 queda para coordinación con el equipo/docente. No se implementa una versión alternativa en .NET 8.
- EF Core e Identity EF 10.0.11, Npgsql EF 10.0.3, MediatR 12.5.0 y Swashbuckle 10.3.1, con versiones fijadas y restore verificado. Referencias de paquetes: https://www.nuget.org/packages/MediatR/12.5.0 y https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3.
- API -> Application + Infrastructure; Infrastructure -> Application + Domain; Application -> Domain; Domain no tiene referencias de proyecto ni paquetes externos. Tests es un proyecto de verificación adicional a las cuatro capas.
- Se conserva la solución original de Rider y su carpeta. Se verificó que Class1.cs era únicamente la plantilla vacía antes de retirarla. La biblioteca original se reemplazó por las cuatro capas.
- Un host MVC/API, cookies Identity, roles y políticas. Mutaciones MVC y API requieren antiforgery; API no redirige al login. Swagger solo en Development. No JWT ni registro público.
- Casos de uso en archivos con record e internal sealed Handler. Los controladores no consultan EF. Repository/UoW se limita por tipo a Categoria y Producto; movimientos y ventas quedan fuera de CRUD genérico.
- Categoria tiene nombre normalizado calculado e índice único para evitar depender de collation o conversión Unicode del motor. Producto normaliza su código. No hay navegaciones serializables, contraseñas ni IdentityUser en las respuestas del catálogo.
- Relaciones restrictivas para conservar historial, cantidades/peso decimal(18,3), dinero decimal(18,2), tipo de cambio decimal(18,6), fechas UTC. Stock por producto y almacén con índice único, sin stock editable en Producto.
- La compra/importación desde proveedores nacionales o internacionales es distinta de una OrdenVenta destinada a un cliente internacional. La venta captura moneda, tipo de cambio, Incoterm y país de destino; no consulta tasas externas ni inventa impuestos.
- Las transiciones modeladas son Pendiente -> Despachado -> Enviado -> Entregado; se admite anular desde Pendiente o Despachado. Estados finales no se reabren. Estas reglas no implementan aún la confirmación/anulación transaccional de stock.
- Inventario y OrdenVenta preparan control de concurrencia PostgreSQL mediante xmin. Antes de exponer ventas se deben implementar: empleado obtenido de identidad, cálculo servidor, reserva/descuento atómico por almacén, rollback por faltantes, claves de idempotencia de operación y devolución única; probar carreras reales con PostgreSQL. El índice de movimientos no sustituye esas pruebas ni esa implementación.
- Movimientos inmutables al guardar por EF; corrección por nuevo ajuste. No hay endpoints de modificación/borrado. No existe aún un trigger que impida escrituras SQL administrativas externas.
- Se consultaron concretamente ProductosController, CreateProductoCommand, GetAllProductosQuery, IGenericRepository, IUnitOfWork, GenericRepository, UnitOfWork, ApplicationDbContext, ServiceRegistrationExtensions y Program.cs del ERP de referencia en un clon temporal externo. Se tomó la organización, sin copiar módulos ni el modelo Database First. Referencia: https://github.com/GuillermoGabrielVilca-Peralta/chilltechnologies-erp.
- SQLite en memoria verifica aplicación, restricciones y HTTP. No simula las garantías de PostgreSQL/xmin. La base ImportNet original no se toca sin acceso a secretos y revisión de su esquema.
