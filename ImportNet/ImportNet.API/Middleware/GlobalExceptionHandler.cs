using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Middleware;

/// <summary>
/// Traduce excepciones de dominio, validación, seguridad y servicios externos a ProblemDetails (RFC 7807).
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problems,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is ValidacionException validacion)
        {
            logger.LogWarning("Validación fallida en {Path}: {Errores}", context.Request.Path, validacion.Errores.Keys);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var detalles = new ValidationProblemDetails(new Dictionary<string, string[]>(validacion.Errores))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validacion.Message,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };

            return await problems.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = detalles
            });
        }

        var status = exception switch
        {
            ReglaNegocioException => StatusCodes.Status400BadRequest,
            NoEncontradoException => StatusCodes.Status404NotFound,
            ConflictoException => StatusCodes.Status409Conflict,
            AccesoNoPermitidoException acceso => acceso.Status,
            ServicioExternoException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status >= 500)
            logger.LogError(exception, "Error no controlado en {Path} (TraceId: {TraceId})", context.Request.Path, context.TraceIdentifier);
        else
            logger.LogWarning("Error controlado ({Status}) en {Path}: {Message}", status, context.Request.Path, exception.Message);

        context.Response.StatusCode = status;

        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == StatusCodes.Status500InternalServerError
                    ? "No se pudo completar la solicitud."
                    : exception.Message,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
