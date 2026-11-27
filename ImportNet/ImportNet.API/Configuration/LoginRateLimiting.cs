using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Configuration;

public static class LoginRateLimiting
{
    public const string Policy = "Login";

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var limite = config.GetValue("LoginRateLimit:PermitLimit", 10);
        var segundos = config.GetValue("LoginRateLimit:WindowSeconds", 60);

        if (limite is < 1 or > 100 || segundos is < 1 or > 3600)
            throw new InvalidOperationException("LoginRateLimit requiere PermitLimit entre 1 y 100 y WindowSeconds entre 1 y 3600.");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = TimeSpan.FromSeconds(segundos),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                {
                    http.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);
                }

                var problems = http.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiados intentos. Espere antes de volver a iniciar sesión.",
                        Extensions = { ["traceId"] = http.TraceIdentifier }
                    }
                });
            };
        });

        return services;
    }
}
