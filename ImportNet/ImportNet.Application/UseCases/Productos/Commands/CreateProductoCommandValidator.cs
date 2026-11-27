using FluentValidation;

namespace ImportNet.Application.UseCases.Productos.Commands;

internal sealed class CreateProductoCommandValidator : AbstractValidator<CreateProductoCommand>
{
    public CreateProductoCommandValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnidadMedida).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PrecioVenta).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoriaId).GreaterThan(0);
        RuleFor(x => x.Descripcion).MaximumLength(1000).When(x => x.Descripcion is not null);
        RuleFor(x => x.PartidaArancelaria).MaximumLength(20).When(x => x.PartidaArancelaria is not null);
    }
}
