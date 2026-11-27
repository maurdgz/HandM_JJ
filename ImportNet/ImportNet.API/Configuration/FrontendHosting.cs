namespace ImportNet.API.Configuration;

public static class FrontendHosting
{
    public static IApplicationBuilder UseFrontend(this IApplicationBuilder app)
    {
        return app.UseFileServer(new FileServerOptions
        {
            EnableDirectoryBrowsing = false,
            StaticFileOptions =
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.CacheControl =
                        context.Context.Request.Path.StartsWithSegments("/_astro")
                            ? "public,max-age=31536000,immutable"
                            : "no-store";
                }
            }
        });
    }
}
