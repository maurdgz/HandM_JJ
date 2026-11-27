using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface ICompraRepository
{
    void Agregar(Compra compra);
    void MarcarEdicion(Compra compra);
    void Eliminar(Compra compra);
    Task<Pagina<Compra>> Listar(int pagina, int tamano, CancellationToken ct);
    Task<Compra?> Obtener(int id, CancellationToken ct);
}
