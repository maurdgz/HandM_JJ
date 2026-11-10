namespace ImportNet.Domain.Interfaces;

public interface IUnitOfWork
{
    ICategoriaRepository Categorias { get; }
    IProductoRepository Productos { get; }
    Task<int> Guardar(CancellationToken ct);
}
