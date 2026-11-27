using FluentValidation;

namespace ImportNet.Application.UseCases.AsistenteIa;

internal sealed class AnalizarNegocioIaCommandValidator : AbstractValidator<AnalizarNegocioIaCommand>
{
    public AnalizarNegocioIaCommandValidator()
    {
        RuleFor(x => x.Pregunta)
            .MaximumLength(500)
            .WithMessage("La pregunta admite hasta 500 caracteres.");

        RuleFor(x => x)
            .Must(x => x.Desde is null || x.Hasta is null || x.Desde <= x.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
    }
}
