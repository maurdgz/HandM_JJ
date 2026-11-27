namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    ICompraRepository Compras { get; }
    IClienteRepository Clientes { get; }
    IProveedorRepository Proveedores { get; }
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    IInventarioRepository Inventarios { get; }
    IMovimientoRepository Movimientos { get; }
    IReporteRepository Reportes { get; }
    IVentaRepository Ventas { get; }
    IEmpleadoRepository Empleados { get; }

    Task<int> GuardarAsync(CancellationToken ct = default);

    Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}
