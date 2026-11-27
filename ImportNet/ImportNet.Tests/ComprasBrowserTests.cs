using System.Diagnostics;
using System.Text.Json;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace ImportNet.Tests;
public sealed class ComprasBrowserTests
{
    [BrowserFact, Trait("Browser", "Chrome"), Trait("Database", "PostgreSQL")]
    public async Task Astro_LoginCrudRecibidaRolesY401()
    {
        using var factory = new DashboardPostgreSqlFactory(); using var client = TestSupport.CreateKestrelClient(factory);
        var admin = await AuthApiTests.CrearUsuario(factory, "Administrador"); var seller = await AuthApiTests.CrearUsuario(factory, "Vendedor");
        var datos = await ComprasApiTests.Preparar(factory); int recibidaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c = new Compra(datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas);
            db.Add(c); db.Entry(c).Property(x => x.Estado).CurrentValue = EstadoCompra.Recibida; await db.SaveChangesAsync(); recibidaId = c.Id;
        }
        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_compras_browser.py"));
        var start = new ProcessStartInfo("python") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(script);
        using var process = Process.Start(start)!;
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { apiUrl = client.BaseAddress!.ToString(), admin = new { email = admin.Email, password = admin.Password }, seller = new { email = seller.Email, password = seller.Password }, proveedorId = datos.ProveedorId, productoId = datos.Lineas[0].ProductoId, recibidaId })); process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException("Tiempo agotado en Astro browser."); }
        await Task.WhenAll(output, errors);
        var diagnostic = (await errors).Split('\n').FirstOrDefault(x => x.StartsWith("Browser verification failed at "));
        Assert.True(process.ExitCode == 0, "Fallo Astro browser. " + (diagnostic ?? "Python error: " + string.Join(" | ", (await errors).Split('\n').Where(x => x.StartsWith("UnicodeEncodeError") || x.StartsWith("ModuleNotFoundError") || x.StartsWith("TimeoutError"))))); Console.WriteLine(await output);
        using var check = factory.Services.CreateScope(); var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(9, (await context.Set<Inventario>().SingleAsync()).Cantidad); Assert.Equal(0, (await context.Set<Producto>().SingleAsync()).CostoPromedio);
        Assert.Empty(await context.Set<MovimientoInventario>().ToListAsync()); Assert.Single(await context.Set<Compra>().ToListAsync());
    }
}
