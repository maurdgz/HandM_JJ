using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record UpdateProductoCommand(
    int Id,
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
    decimal? TipoCambioImportacion = null) : IRequest<Unit>;

internal sealed class UpdateProductoCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductoCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await unitOfWork.Productos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Producto no encontrado.");

        producto.Editar(
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

        await unitOfWork.GuardarAsync(cancellationToken);
        return Unit.Value;
    }
}
