namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompraRepository Compras { get; }
    ITercerosRepository Terceros { get; }
    IClienteRepository Clientes { get; }
    IProveedorRepository Proveedores { get; }
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    IVentaRepository Ventas { get; }
    IInventarioRepository Inventarios { get; }
    IMovimientoRepository Movimientos { get; }

    Task<int> Guardar(CancellationToken ct);

    Task<T> EjecutarEnTransaccion<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken ct);
}
