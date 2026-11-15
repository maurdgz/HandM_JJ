using ImportNet.Domain.Entities;
namespace ImportNet.Domain.Interfaces;
public interface ICompraRepository
{
    Task<Pagina<Compra>> Listar(int pagina, int tamano, CancellationToken ct);
    Task<Compra?> Obtener(int id, CancellationToken ct);
}
