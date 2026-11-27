using FluentValidation;

namespace ImportNet.Application.UseCases.Auth;

internal sealed class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(254).WithMessage("El correo admite hasta 254 caracteres.")
            .EmailAddress().WithMessage("Correo inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(1024).WithMessage("La contraseña excede el límite permitido.");
    }
}
