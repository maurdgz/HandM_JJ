using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImportNet.Application.Common.Behaviors;

/// <summary>
/// Registra el tiempo de ejecución y los fallos de cada comando o consulta en MediatR.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var nombre = typeof(TRequest).Name;
        var reloj = Stopwatch.StartNew();

        try
        {
            var respuesta = await next(cancellationToken);
            reloj.Stop();
            logger.LogInformation("Solicitud {RequestName} completada en {ElapsedMs} ms", nombre, reloj.ElapsedMilliseconds);
            return respuesta;
        }
        catch (Exception ex)
        {
            reloj.Stop();
            logger.LogWarning(ex, "Solicitud {RequestName} falló tras {ElapsedMs} ms", nombre, reloj.ElapsedMilliseconds);
            throw;
        }
    }
}
