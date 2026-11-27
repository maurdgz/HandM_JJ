using System.Diagnostics;
using System.Text.Json;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class DashboardBrowserTests
{
    [BrowserFact, Trait("Browser", "Chrome"), Trait("Database", "PostgreSQL")]
    public Task Astro_PosDashboardRolesPeriodosYEstados_ContraPostgreSql() => Ejecutar(false);

    [BrowserFact, Trait("Browser", "Chrome"), Trait("Database", "PostgreSQL")]
    public Task UnSoloHost_PosDashboardYPermisos_ContraPostgreSql() => Ejecutar(true);

    private static async Task Ejecutar(bool singleHost)
    {
        using var factory = new DashboardPostgreSqlFactory();
        using var client = TestSupport.CreateKestrelClient(factory);
        var admin = await AuthApiTests.CrearUsuario(factory, "Administrador");
        var seller = await AuthApiTests.CrearUsuario(factory, "Vendedor");
        var (producto, segundo) = await PosPostgreSqlTests.Preparar(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Add(new Cliente { Nombre = "Cliente navegador", NumeroDocumento = "90909090", TipoDocumento = TipoDocumento.Dni });
            var empleado = await db.Set<Empleado>().SingleAsync(x => x.UsuarioId == admin.Id);
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            var anterior = new DateTime(hoy.Year, hoy.Month, 1, 5, 0, 0, DateTimeKind.Utc).AddDays(-1);
            var venta = new Venta(1, empleado.Id, 1, 40, 0, anterior);
            db.Add(venta); await db.SaveChangesAsync();
            db.Add(new DetalleVenta(producto, 1, 40, 0) { VentaId = venta.Id });
            await db.SaveChangesAsync();
        }
        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_dashboard_browser.py"));
        var start = new ProcessStartInfo("python") { RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(script);
        using var process = Process.Start(start)!;
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new {
            singleHost, apiUrl = client.BaseAddress!.ToString(), admin = new { email = admin.Email, password = admin.Password },
            seller = new { email = seller.Email, password = seller.Password }, producto, segundo
        }));
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException("Tiempo agotado al validar dashboard en Chrome."); }
        await Task.WhenAll(output, errors);
        var diagnostic = (await errors).Split('\n').FirstOrDefault(x => x.StartsWith("Dashboard browser failed at "));
        Assert.True(process.ExitCode == 0, diagnostic ?? "Falló el proceso de navegador del dashboard.");
        Console.WriteLine(await output);
        using var check = factory.Services.CreateScope();
        var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1m, (await context.Set<Inventario>().SingleAsync(x => x.ProductoId == producto)).Cantidad);
        Assert.Equal(0m, (await context.Set<Inventario>().SingleAsync(x => x.ProductoId == segundo)).Cantidad);
        Assert.Equal(3, await context.Set<Venta>().CountAsync()); // Histórica, anulada y vendedor.
        Assert.Equal(3, await context.Set<MovimientoInventario>().CountAsync()); // Salida, reversión y salida.
    }
}
