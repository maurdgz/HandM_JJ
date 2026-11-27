using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ImportNet.Infrastructure.Persistence;

/// <summary>
/// Resuelve cadenas de conexión locales y URLs de PostgreSQL en la nube (Render, Neon, Supabase).
/// </summary>
public static class ConnectionStringResolver
{
    private const string DefaultLocalConnection = "Host=localhost;Database=ImportNet;Username=postgres";

    public static string Resolver(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var configurada = config.GetConnectionString("ImportNet");
        if (!string.IsNullOrWhiteSpace(configurada))
            return Normalizar(configurada.Trim());

        var databaseUrl = config["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return Normalizar(databaseUrl.Trim());

        return DefaultLocalConnection;
    }

    public static string Normalizar(string valor)
    {
        if (!valor.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !valor.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return valor;
        }

        var uri = new Uri(valor);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/')
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var partes = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(partes[0]);
            if (partes.Length > 1)
                builder.Password = Uri.UnescapeDataString(partes[1]);
        }

        var esLocal = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host == "127.0.0.1";

        builder.SslMode = esLocal ? SslMode.Prefer : SslMode.Require;

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            foreach (var par in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = par.Split('=', 2);
                var clave = Uri.UnescapeDataString(kv[0]).ToLowerInvariant();
                var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";

                if (clave == "sslmode" && Enum.TryParse<SslMode>(val, true, out var sslMode))
                    builder.SslMode = sslMode;
            }
        }

        return builder.ConnectionString;
    }
}
