using ImportNet.API.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Configuration;

public static class ApiResponses
{
    public static IApplicationBuilder UseApiResponses(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app.UseStatusCodePages(async statusContext =>
        {
            var http = statusContext.HttpContext;
            if (!http.Request.Path.StartsWithSegments("/api"))
                return;

            var status = http.Response.StatusCode;
            var titulo = status switch
            {
                401 => "Debe iniciar sesión con una cuenta válida.",
                403 => "No tiene permiso para realizar esta acción.",
                404 => "No se encontró el recurso solicitado.",
                405 => "Método no permitido.",
                _ => "No se pudo completar la solicitud."
            };

            var problemas = http.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemas.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = titulo,
                    Extensions = { ["traceId"] = http.TraceIdentifier }
                }
            });
        });
    }
}
