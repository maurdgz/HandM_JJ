using FluentValidation;

namespace ImportNet.Application.UseCases.Categorias.Commands;

internal sealed class UpdateCategoriaCommandValidator : AbstractValidator<UpdateCategoriaCommand>
{
    public UpdateCategoriaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Identificador inválido.");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre es obligatorio.")
            .MaximumLength(100).WithMessage("Nombre admite hasta 100 caracteres.");
    }
}
