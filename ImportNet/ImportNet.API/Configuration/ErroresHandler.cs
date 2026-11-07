using ImportNet.Domain.Entities;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.Security;
namespace ImportNet.API.Configuration;

internal sealed class ErroresHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken ct)
    {
        var status = ex switch {
            ReglaNegocioException => 400,
            NoEncontradoException => 404,
            ConflictoException => 409,
            AccesoNoPermitidoException acceso => acceso.Status,
            _ => 500
        };
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext {
            HttpContext = context,
            ProblemDetails = new ProblemDetails {
                Status = status,
                Title = status == 500 ? "No se pudo completar la solicitud." : ex.Message,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
