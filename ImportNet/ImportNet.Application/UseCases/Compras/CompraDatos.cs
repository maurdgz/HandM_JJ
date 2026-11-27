using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;

namespace ImportNet.Application.UseCases.Compras;

public sealed record CompraDatos(int ProveedorId, DateTime Fecha, decimal TipoCambio, decimal FletePen, decimal AduanaPen,
    IReadOnlyList<LineaCompra> Lineas)
{
    internal async Task ValidarReferencias(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var proveedor = await unitOfWork.Proveedores.Proveedor(ProveedorId, ct);
        if (proveedor == null || !proveedor.Activo)
            throw new ReglaNegocioException("Seleccione un proveedor activo existente.");

        if (Lineas == null || Lineas.Count == 0 || Lineas.Count > 100 || Lineas.Any(l => l == null))
            throw new ReglaNegocioException("Indique de 1 a 100 lineas validas.");

        foreach (var linea in Lineas)
        {
            var producto = await unitOfWork.Productos.Obtener(linea.ProductoId, ct);
            if (producto == null || !producto.Activo)
                throw new ReglaNegocioException("Seleccione productos activos existentes.");
        }
    }
}
