using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Configuration;

public sealed class ErroresHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemas;

    public ErroresHandler(IProblemDetailsService problemas)
    {
        _problemas = problemas;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = 500;
        if (exception is ReglaNegocioException) status = 400;
        if (exception is NoEncontradoException) status = 404;
        if (exception is ConflictoException) status = 409;
        if (exception is AccesoNoPermitidoException acceso) status = acceso.Status;

        context.Response.StatusCode = status;

        return await _problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "No se pudo completar la solicitud." : exception.Message
            }
        });
    }
}
