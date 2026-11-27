using ImportNet.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImportNet.Infrastructure.Persistence;

public static class DatabaseStartupExtensions
{
    public static async Task AplicarMigracionesSiCorrespondeAsync(
        IServiceProvider services,
        IConfiguration config,
        ILogger logger,
        CancellationToken ct = default)
    {
        var aplicarMigraciones = config.GetValue<bool>("IMPORTNET_APPLY_MIGRATIONS")
            || config.GetValue<bool>("Database:MigrateOnStartup");

        if (aplicarMigraciones)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            logger.LogInformation("Aplicando migraciones pendientes de PostgreSQL...");
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("Migraciones de base de datos al día.");
        }

        var crearAdmin = config.GetValue<bool>("Bootstrap:AutoCreateOnStartup")
            || config.GetValue<bool>("Bootstrap:OnStartup");

        if (crearAdmin)
        {
            await AdminBootstrapper.EjecutarSiNoExisteAdministradorAsync(services, config, logger, ct);
        }
    }
}
