namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompraRepository Compras { get; }
    ITercerosRepository Terceros { get; }
    IClienteRepository Clientes { get; }
    IProveedorRepository Proveedores { get; }
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    IInventarioRepository Inventarios { get; }
    IMovimientoRepository Movimientos { get; }
    IReporteRepository Reportes { get; }
    IVentaRepository Ventas { get; }

    Task<int> Guardar(CancellationToken ct);

    Task<T> EjecutarEnTransaccion<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken ct);
}