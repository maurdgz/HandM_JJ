using FluentValidation;
using ImportNet.Domain.Exceptions;
using MediatR;

namespace ImportNet.Application.Common.Behaviors;

/// <summary>
/// Ejecuta automáticamente los validadores FluentValidation antes de invocar al Handler.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var contexto = new ValidationContext<TRequest>(request);
        var resultados = await Task.WhenAll(validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));

        var errores = resultados
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        if (errores.Count > 0)
            throw new ValidacionException(errores);

        return await next(cancellationToken);
    }
}
