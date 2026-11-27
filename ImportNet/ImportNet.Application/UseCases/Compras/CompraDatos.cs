using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;

namespace ImportNet.Application.UseCases.Compras;

public sealed record CompraDatos(
    int ProveedorId,
    DateTime Fecha,
    decimal TipoCambio,
    decimal FletePen,
    decimal AduanaPen,
    IReadOnlyList<LineaCompra> Lineas)
{
    internal async Task ValidarReferenciasAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var proveedor = await unitOfWork.Proveedores.ObtenerPorIdAsync(ProveedorId, ct);
        if (proveedor == null || !proveedor.Activo)
            throw new ReglaNegocioException("Seleccione un proveedor activo existente.");

        if (Lineas == null || Lineas.Count == 0 || Lineas.Count > 100 || Lineas.Any(l => l == null))
            throw new ReglaNegocioException("Indique de 1 a 100 lineas validas.");

        var productoIds = Lineas.Select(l => l.ProductoId).Distinct().ToList();
        var productos = await unitOfWork.Productos.ObtenerPorIdsAsync(productoIds, ct);

        foreach (var productoId in productoIds)
        {
            if (!productos.TryGetValue(productoId, out var producto) || !producto.Activo)
                throw new ReglaNegocioException("Seleccione productos activos existentes.");
        }
    }
}
