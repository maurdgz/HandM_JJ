namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompraRepository Compras { get; }
    ITercerosRepository Terceros { get; }
    IClienteRepository Clientes { get; }
    IProveedorRepository Proveedores { get; }
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    Task<int> Guardar(CancellationToken ct);
}
