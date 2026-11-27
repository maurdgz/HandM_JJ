namespace ImportNet.API.Configuration;

internal static class FrontendHosting
{
    public static IApplicationBuilder UseFrontend(this IApplicationBuilder app)
    {
        // Astro genera un index.html por ruta; no se redirigen rutas API a HTML.
        return app.UseFileServer(new FileServerOptions {
            EnableDirectoryBrowsing = false,
            StaticFileOptions = {
                OnPrepareResponse = context => {
                    context.Context.Response.Headers.CacheControl =
                        context.Context.Request.Path.StartsWithSegments("/_astro")
                            ? "public,max-age=31536000,immutable"
                            : "no-store";
                }
            }
        });
    }
}
