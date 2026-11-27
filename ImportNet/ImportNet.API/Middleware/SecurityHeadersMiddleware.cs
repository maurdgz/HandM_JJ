namespace ImportNet.API.Middleware;

/// <summary>
/// Aplica cabeceras de seguridad HTTP estándar y deshabilita caché para endpoints de API.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            if (context.Request.Path.StartsWithSegments("/api"))
                headers.CacheControl = "no-store";

            return Task.CompletedTask;
        });

        await next(context);
    }
}
