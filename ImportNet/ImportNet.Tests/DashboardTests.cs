using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class DashboardTests
{
    [Fact]
    public async Task Indicadores_CoincidenConReportesYExcluyenAnuladasFueraDePeriodo()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await VerificarIndicadores(factory, client);
    }

    internal static async Task VerificarIndicadores(WebApplicationFactory<Program> factory, HttpClient client)
    {
        await SmokeTests.Login(factory, client, "Administrador");
        var me = (await client.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Categoria("Dashboard " + Guid.NewGuid().ToString("N"));
        db.Add(categoria);
        await db.SaveChangesAsync();
        var a = new Producto(Guid.NewGuid().ToString("N"), "Producto A", null, "Unidad", null, 100, null, categoria.Id, 2);
        var b = new Producto(Guid.NewGuid().ToString("N"), "Producto B", null, "Unidad", null, 60, null, categoria.Id, 1);
        var c = new Producto(Guid.NewGuid().ToString("N"), "Sin inventario", null, "Unidad", null, 10, null, categoria.Id, 0);
        var inactivo = new Producto(Guid.NewGuid().ToString("N"), "Inactivo", null, "Unidad", null, 10, null, categoria.Id, 10);
        a.ActualizarCostoPromedioPorRecepcion(0, 1, 10);
        b.ActualizarCostoPromedioPorRecepcion(0, 1, 20);
        inactivo.CambiarEstado(false);
        db.AddRange(a, b, c, inactivo);
        await db.SaveChangesAsync();
        db.AddRange(new Inventario { ProductoId = a.Id, AlmacenId = 1, Cantidad = 2 },
            new Inventario { ProductoId = b.Id, AlmacenId = 1, Cantidad = 4 });
        var inicio = new DateTime(2026, 11, 27, 5, 0, 0, DateTimeKind.Utc);
        var mes = new DateTime(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc);
        var numero = (await db.Set<Venta>().MaxAsync(x => (int?)x.Numero) ?? 0) + 1;
        async Task Agregar(DateTime fecha, Producto producto, decimal precio, decimal descuento = 0, bool anulada = false)
        {
            var venta = new Venta(numero++, me.EmpleadoId, 1, precio, descuento, fecha);
            if (anulada) venta.Anular("Prueba de indicadores");
            db.Add(venta);
            await db.SaveChangesAsync();
            db.Add(new DetalleVenta(producto.Id, 1, precio, descuento) { VentaId = venta.Id });
            await db.SaveChangesAsync();
        }
        await Agregar(inicio, a, 100, 10);
        await Agregar(inicio.AddHours(1), b, 60);
        await Agregar(inicio.AddTicks(-10), a, 30);
        await Agregar(mes.AddTicks(-10), a, 50);
        await Agregar(inicio.AddDays(1), a, 500);
        await Agregar(inicio, a, 70, anulada: true);
        var resultado = await scope.ServiceProvider.GetRequiredService<IDashboardRepository>()
            .Resumen(new DateOnly(2026, 11, 27), inicio, inicio.AddDays(1), mes, default);
        Assert.Equal(150m, resultado.VentasHoy);
        Assert.Equal(2, resultado.CantidadVentasHoy);
        Assert.Equal(180m, resultado.VentasMes);
        Assert.Equal(3, resultado.CantidadVentasMes);
        Assert.Equal(60m, resultado.TicketPromedioMes);
        Assert.Equal(140m, resultado.MargenMes);
        Assert.Equal(3, resultado.ProductosActivos);
        Assert.Equal(2, resultado.ProductosBajoMinimo);
        Assert.Equal(1, resultado.ProductosSinStock);
        const string periodo = "desde=2026-11-01&hasta=2026-11-27";
        var ventas = (await client.GetFromJsonAsync<VentasPorPeriodo[]>($"/api/reportes/ventas-por-periodo?{periodo}&agrupacion=Mes"))!;
        Assert.Equal(resultado.VentasMes, Assert.Single(ventas).Total);
        Assert.Equal("2026-11", ventas[0].Periodo);
        var dias = (await client.GetFromJsonAsync<VentasPorPeriodo[]>($"/api/reportes/ventas-por-periodo?{periodo}"))!;
        Assert.Equal(30m, dias.Single(x => x.Periodo == "2026-11-26").Total);
        Assert.Equal(150m, dias.Single(x => x.Periodo == "2026-11-27").Total);
        var top = (await client.GetFromJsonAsync<TopProducto[]>($"/api/reportes/top-productos?{periodo}&top=1"))!;
        Assert.Equal(a.Id, Assert.Single(top).ProductoId);
        Assert.Equal(2m, top[0].Cantidad);
        var margen = (await client.GetFromJsonAsync<MargenProducto[]>($"/api/reportes/margen-productos?{periodo}"))!;
        Assert.Equal(resultado.MargenMes, margen.Sum(x => x.Margen));
        var vendedores = (await client.GetFromJsonAsync<VentasPorVendedor[]>($"/api/reportes/ventas-por-vendedor?{periodo}"))!;
        Assert.Equal(resultado.VentasMes, Assert.Single(vendedores).Total);
    }

    [Fact]
    public async Task Permisos_ResumenPropioNoExponeInformacionAdministrativa()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var vendedor = factory.CreateClient();
        factory.InitializeSchema();
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/reportes/dashboard")).StatusCode);
        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, vendedor, "Vendedor");
        var me = (await admin.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Add(new Venta(1, me.EmpleadoId, 1, 100, 10, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }
        var resumen = (await admin.GetFromJsonAsync<ResumenDashboard>("/api/reportes/dashboard"))!;
        Assert.Equal(90m, resumen.VentasHoy);
        foreach (var ruta in new[] { "dashboard", "ventas-por-periodo", "ventas-por-vendedor", "top-productos", "margen-productos", "stock-bajo" })
            Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.GetAsync($"/api/reportes/{ruta}")).StatusCode);
        var propio = (await vendedor.GetFromJsonAsync<CierreCaja>("/api/reportes/mi-resumen?empleadoId=" + me.EmpleadoId))!;
        Assert.Equal(0, propio.Ventas);
        Assert.Equal(0m, propio.Total);
        Assert.DoesNotContain("margen", await vendedor.GetStringAsync("/api/reportes/mi-resumen"), StringComparison.OrdinalIgnoreCase);
    }
}
