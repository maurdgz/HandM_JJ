using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record CreateProductoCommand(
    string Codigo,
    string Nombre,
    string? Descripcion,
    string UnidadMedida,
    decimal? Peso,
    decimal PrecioVenta,
    string? PartidaArancelaria,
    int CategoriaId,
    decimal StockMinimo,
    decimal? CostoImportacionUsd = null,
    decimal? TipoCambioImportacion = null) : IRequest<int>;

internal sealed class CreateProductoCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductoCommand, int>
{
    public async Task<int> Handle(CreateProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = new Producto(
            request.Codigo,
            request.Nombre,
            request.Descripcion,
            request.UnidadMedida,
            request.Peso,
            request.PrecioVenta,
            request.PartidaArancelaria,
            request.CategoriaId,
            request.StockMinimo);
        producto.RegistrarCostoImportacion(request.CostoImportacionUsd, request.TipoCambioImportacion);

        if (!await unitOfWork.Categorias.ExisteAsync(request.CategoriaId, cancellationToken))
            throw new ReglaNegocioException("La categoría no existe.");

        if (await unitOfWork.Productos.ExisteCodigoAsync(producto.Codigo, producto.Id, cancellationToken))
            throw new ConflictoException("Ya existe un producto con ese código.");

        unitOfWork.Productos.Agregar(producto);
        await unitOfWork.GuardarAsync(cancellationToken);
        return producto.Id;
    }
}
