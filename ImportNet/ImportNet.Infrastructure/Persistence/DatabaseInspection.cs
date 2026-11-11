using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ImportNet.Infrastructure.Persistence;

public static class DatabaseInspection
{
    // Explicit read-only diagnostic: never applies migrations or changes existing data.
    public static async Task Ejecutar(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.OpenConnectionAsync();
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT current_database(), current_user";
        string database, user;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            await reader.ReadAsync();
            database = reader.GetString(0); user = reader.GetString(1);
        }
        if (database != "ImportNet" || user != "postgres")
            throw new InvalidOperationException("La conexión debe apuntar a ImportNet con el usuario postgres.");
        command.CommandText = """
            SELECT table_schema || '.' || table_name FROM information_schema.tables
            WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name
            """;
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        var applied = tables.Contains("public.__EFMigrationsHistory")
            ? (await db.Database.GetAppliedMigrationsAsync()).ToArray() : Array.Empty<string>();
        var pending = db.Database.GetMigrations().Except(applied).ToArray();
        var counts = new Dictionary<string, long>();
        foreach (var table in new[] { "Producto", "Cliente", "OrdenVenta", "DetalleOrdenVenta", "Almacen", "MovimientoInventario" })
        {
            if (!tables.Contains("public." + table)) continue;
            command.CommandText = $"SELECT count(*) FROM public.\"{table}\"";
            counts[table] = (long)(await command.ExecuteScalarAsync())!;
        }
        long? userCount = tables.Contains("public.AspNetUsers") ? await db.Users.LongCountAsync() : null;
        long? employeeCount = tables.Contains("public.Empleado") ? await db.Set<ImportNet.Domain.Entities.Empleado>().LongCountAsync() : null;
        Console.WriteLine(JsonSerializer.Serialize(new { Database = database, User = user, Tables = tables, AppliedMigrations = applied,
            PendingMigrations = pending, Users = userCount, Employees = employeeCount, BusinessRows = counts }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
