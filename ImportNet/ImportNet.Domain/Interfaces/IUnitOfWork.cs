namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork
{
    ITercerosRepository Terceros { get; }
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    Task<int> Guardar(CancellationToken ct);
}
