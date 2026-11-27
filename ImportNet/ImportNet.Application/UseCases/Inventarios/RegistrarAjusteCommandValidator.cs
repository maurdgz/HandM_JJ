using FluentValidation;

namespace ImportNet.Application.UseCases.Inventarios;

internal sealed class RegistrarAjusteCommandValidator : AbstractValidator<RegistrarAjusteCommand>
{
    public RegistrarAjusteCommandValidator()
    {
        RuleFor(x => x.ProductoId).GreaterThan(0).WithMessage("Indique un producto válido.");
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indique el motivo del ajuste.");
        RuleFor(x => x.Observacion).MaximumLength(300).When(x => x.Observacion is not null);
    }
}
