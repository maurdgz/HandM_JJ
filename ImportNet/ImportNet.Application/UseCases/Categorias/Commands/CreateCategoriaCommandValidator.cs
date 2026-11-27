using FluentValidation;

namespace ImportNet.Application.UseCases.Categorias.Commands;

internal sealed class CreateCategoriaCommandValidator : AbstractValidator<CreateCategoriaCommand>
{
    public CreateCategoriaCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre es obligatorio.")
            .MaximumLength(100).WithMessage("Nombre admite hasta 100 caracteres.");
    }
}
