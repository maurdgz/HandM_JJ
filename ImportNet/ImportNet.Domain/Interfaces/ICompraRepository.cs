using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface ICompraRepository : IGenericRepository<Compra>
{
    void MarcarEdicion(Compra compra);
    Task<Pagina<Compra>> ListarAsync(int pagina, int tamano, CancellationToken ct = default);
    Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default);
}
