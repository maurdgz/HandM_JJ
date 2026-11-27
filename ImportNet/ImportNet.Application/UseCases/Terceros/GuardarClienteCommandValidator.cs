using FluentValidation;
using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Terceros;

internal sealed class GuardarClienteCommandValidator : AbstractValidator<GuardarClienteCommand>
{
    public GuardarClienteCommandValidator()
    {
        RuleFor(x => x.TipoDocumento).IsInEnum().WithMessage("Tipo de documento inválido.");
        RuleFor(x => x.NumeroDocumento)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .Must(d => d != null && d.All(char.IsAsciiDigit))
            .WithMessage("El documento debe contener solo dígitos.")
            .Must((cmd, doc) => doc != null && doc.Length == (cmd.TipoDocumento == TipoDocumento.Dni ? 8 : 11))
            .WithMessage("DNI requiere 8 dígitos y RUC 11 dígitos.");
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Telefono).MaximumLength(20).When(x => x.Telefono is not null);
    }
}
