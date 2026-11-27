using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Configuration;

internal static class ApiResponses
{
    public static IApplicationBuilder UseApiResponses(this IApplicationBuilder app)
    {
        app.Use(async (context, next) => {
            context.Response.OnStarting(() => {
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers.XFrameOptions = "DENY";
                if (context.Request.Path.StartsWithSegments("/api"))
                    context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            });
            await next(context);
        });
        return app.UseStatusCodePages(async statusContext => {
            var http = statusContext.HttpContext;
            if (!http.Request.Path.StartsWithSegments("/api")) return;
            var status = http.Response.StatusCode;
            await http.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new ProblemDetailsContext {
                HttpContext = http,
                ProblemDetails = new ProblemDetails {
                    Status = status,
                    Title = status switch {
                        401 => "Debe iniciar sesión con una cuenta válida.",
                        403 => "No tiene permiso para realizar esta acción.",
                        404 => "No se encontró el recurso solicitado.",
                        405 => "Método no permitido.",
                        _ => "No se pudo completar la solicitud."
                    }
                }
            });
        });
    }
}
