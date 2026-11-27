using FluentValidation;

namespace ImportNet.Application.UseCases.Ventas;

internal sealed class AnularVentaCommandValidator : AbstractValidator<AnularVentaCommand>
{
    public AnularVentaCommandValidator()
    {
        RuleFor(x => x.VentaId).GreaterThan(0).WithMessage("Indique una venta válida.");
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indique el motivo de la anulación.").MaximumLength(300);
    }
}
