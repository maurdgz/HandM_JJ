using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record CompraDatos(int ProveedorId, DateTime Fecha, decimal TipoCambio, decimal FletePen, decimal AduanaPen, IReadOnlyList<LineaCompra> Lineas)
{
    internal async Task ValidarReferencias(IUnitOfWork uow, CancellationToken ct)
    {
        var proveedor = await uow.Terceros.Proveedor(ProveedorId, ct);
        if (proveedor is null || !proveedor.Activo) throw new ReglaNegocioException("Seleccione un proveedor activo existente.");
        if (Lineas is null || Lineas.Count == 0 || Lineas.Count > 100 || Lineas.Any(x => x is null)) throw new ReglaNegocioException("Indique de 1 a 100 lineas validas.");
        foreach (var linea in Lineas)
        {
            var p = await uow.Productos.Obtener(linea.ProductoId, ct);
            if (p is null || !p.Activo) throw new ReglaNegocioException("Seleccione productos activos existentes.");
        }
    }
}
