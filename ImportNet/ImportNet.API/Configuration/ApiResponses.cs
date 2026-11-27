using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Configuration;

public static class ApiResponses
{
    public static IApplicationBuilder UseApiResponses(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.Headers.CacheControl = "no-store";

            await next(context);
        });

        return app.UseStatusCodePages(async statusContext =>
        {
            var http = statusContext.HttpContext;
            if (!http.Request.Path.StartsWithSegments("/api"))
                return;

            var status = http.Response.StatusCode;
            var titulo = "No se pudo completar la solicitud.";
            if (status == 401) titulo = "Debe iniciar sesión con una cuenta válida.";
            if (status == 403) titulo = "No tiene permiso para realizar esta acción.";
            if (status == 404) titulo = "No se encontró el recurso solicitado.";
            if (status == 405) titulo = "Método no permitido.";

            var problemas = http.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemas.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                ProblemDetails = new ProblemDetails { Status = status, Title = titulo }
            });
        });
    }
}
