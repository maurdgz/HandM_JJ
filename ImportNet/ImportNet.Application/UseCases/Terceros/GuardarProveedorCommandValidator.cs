using FluentValidation;

namespace ImportNet.Application.UseCases.Terceros;

internal sealed class GuardarProveedorCommandValidator : AbstractValidator<GuardarProveedorCommand>
{
    public GuardarProveedorCommandValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Contacto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Productos)
            .NotNull()
            .Must(p => p.Distinct().Count() == p.Length)
            .WithMessage("No repita productos para el proveedor.");
    }
}
