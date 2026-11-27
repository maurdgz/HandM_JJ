using FluentValidation;

namespace ImportNet.Application.UseCases.Compras;

internal sealed class CrearCompraCommandValidator : AbstractValidator<CrearCompraCommand>
{
    public CrearCompraCommandValidator()
    {
        RuleFor(x => x.Datos).NotNull().WithMessage("Datos de compra requeridos.");

        When(x => x.Datos is not null, () =>
        {
            RuleFor(x => x.Datos.ProveedorId).GreaterThan(0);
            RuleFor(x => x.Datos.TipoCambio).GreaterThan(0).LessThanOrEqualTo(1000);
            RuleFor(x => x.Datos.FletePen).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Datos.AduanaPen).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Datos.Lineas)
                .NotEmpty().WithMessage("Indique de 1 a 100 lineas.")
                .Must(l => l.Count <= 100).WithMessage("Indique de 1 a 100 lineas.");
        });
    }
}
