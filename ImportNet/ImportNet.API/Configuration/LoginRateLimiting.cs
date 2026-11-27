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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = TimeSpan.FromSeconds(segundos),
                    QueueLimit = 0
                }));

            options.OnRejected = async (context, ct) =>
            {
                var problema = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Demasiados intentos. Espere antes de volver a iniciar sesión."
                };
                await context.HttpContext.Response.WriteAsJsonAsync(problema, ct);
            };
        });

        return services;
    }
}
