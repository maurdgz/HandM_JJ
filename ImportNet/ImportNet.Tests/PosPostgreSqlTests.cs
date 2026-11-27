using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class PosPostgreSqlTests
{
    [PostgreSqlTheory, Trait("Database", "PostgreSQL"), InlineData(true)]
    public async Task UltimaUnidad_DosVentasSimultaneas_SoloUnaPersiste(bool _)
    {
        var barrera = new BarreraStock();
        using var factory = new DashboardPostgreSqlFactory(barrera);
        using var client = TestSupport.CreateKestrelClient(factory);
        await SmokeTests.Login(factory, client, "Vendedor");
        var (producto, _) = await Preparar(factory);
        var venta = new { clienteId = (int?)null, lineas = new[] { new { productoId = producto, cantidad = 1, descuento = 5 } } };
        var respuestas = await Task.WhenAll(client.PostAsJsonAsync("/api/ventas", venta), client.PostAsJsonAsync("/api/ventas", venta));
        Assert.True(barrera.Sincronizada);
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0m, (await db.Set<Inventario>().SingleAsync(x => x.ProductoId == producto)).Cantidad);
        Assert.Equal(95m, (await db.Set<Venta>().SingleAsync()).Total);
        Assert.Single(await db.Set<DetalleVenta>().ToListAsync());
        Assert.Single(await db.Set<MovimientoInventario>().ToListAsync());
    }

    [PostgreSqlTheory, Trait("Database", "PostgreSQL"), InlineData(true)]
    public async Task StockInsuficienteMultilinea_NoPersisteVentaDetalleNiMovimiento(bool _)
    {
        using var factory = new DashboardPostgreSqlFactory();
        using var client = TestSupport.CreateKestrelClient(factory);
        await SmokeTests.Login(factory, client, "Vendedor");
        var (producto, segundo) = await Preparar(factory);
        var respuesta = await client.PostAsJsonAsync("/api/ventas", new { lineas = new[] {
            new { productoId = producto, cantidad = 1, descuento = 0 },
            new { productoId = segundo, cantidad = 2, descuento = 0 }
        } });
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.All(await db.Set<Inventario>().ToListAsync(), x => Assert.Equal(1m, x.Cantidad));
        Assert.Empty(await db.Set<Venta>().ToListAsync());
        Assert.Empty(await db.Set<DetalleVenta>().ToListAsync());
        Assert.Empty(await db.Set<MovimientoInventario>().ToListAsync());
    }

    internal static async Task<(int Producto, int Segundo)> Preparar(DashboardPostgreSqlFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Categoria("POS pruebas");
        db.Add(categoria); await db.SaveChangesAsync();
        var a = new Producto("POS-A", "Producto POS", null, "Unidad", null, 100m, null, categoria.Id, 1);
        var b = new Producto("POS-B", "Segundo POS", null, "Unidad", null, 50m, null, categoria.Id, 0);
        a.ActualizarCostoPromedioPorRecepcion(0, 1, 20m);
        db.AddRange(a, b); await db.SaveChangesAsync();
        db.AddRange(new Inventario { ProductoId = a.Id, AlmacenId = 1, Cantidad = 1 },
            new Inventario { ProductoId = b.Id, AlmacenId = 1, Cantidad = 1 });
        await db.SaveChangesAsync();
        return (a.Id, b.Id);
    }

    private sealed class BarreraStock : DbCommandInterceptor
    {
        private readonly TaskCompletionSource listo = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int llegadas;
        public bool Sincronizada => Volatile.Read(ref llegadas) >= 2;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Inventario\"", StringComparison.Ordinal)
                && Interlocked.Increment(ref llegadas) <= 2)
            {
                if (Volatile.Read(ref llegadas) == 2) listo.TrySetResult();
                await listo.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
