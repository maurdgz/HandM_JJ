using FluentValidation;

namespace ImportNet.Application.UseCases.Ventas;

internal sealed class RegistrarVentaCommandValidator : AbstractValidator<RegistrarVentaCommand>
{
    public RegistrarVentaCommandValidator()
    {
        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("La venta debe tener entre 1 y 100 líneas.")
            .Must(l => l.Count <= 100).WithMessage("La venta debe tener entre 1 y 100 líneas.");

        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.ProductoId).GreaterThan(0);
            linea.RuleFor(l => l.Cantidad).GreaterThan(0).LessThanOrEqualTo(1000000);
            linea.RuleFor(l => l.Descuento).GreaterThanOrEqualTo(0);
        });
    }
}
