using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImportNet.API.Configuration;

internal sealed class LoginRateLimitSettings
{
    public int PermitLimit { get; }
    public int WindowSeconds { get; }

    public LoginRateLimitSettings(IConfiguration config)
    {
        PermitLimit = config.GetValue("LoginRateLimit:PermitLimit", 10);
        WindowSeconds = config.GetValue("LoginRateLimit:WindowSeconds", 60);
        if (PermitLimit is < 1 or > 100 || WindowSeconds is < 1 or > 3600)
            throw new InvalidOperationException("LoginRateLimit requiere PermitLimit entre 1 y 100 y WindowSeconds entre 1 y 3600.");
    }
}

internal static class LoginRateLimiting
{
    public const string Policy = "Login";

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<LoginRateLimitSettings>();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<LoginRateLimitSettings>((options, settings) => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.OnRejected = async (context, _) => {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    http.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new ProblemDetailsContext {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails {
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
