using System.Net;
using System.Net.Http.Json;
using ImportNet.Domain.Entities;
using Xunit;

namespace ImportNet.Tests;

public sealed class ReportesIntegracionVentasTests
{
    [Fact]
    public async Task Anulacion_ActualizaReportesYCajaSinExponerVentasAjenasAlVendedor()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var seller = factory.CreateClient();
        using var anon = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, seller, "Vendedor");
        var propia = await VentasDatosPrueba.Registrar(seller, await VentasDatosPrueba.Catalogo(factory.Services));
        var productosAdmin = await VentasDatosPrueba.Catalogo(factory.Services);
        await VentasDatosPrueba.Registrar(admin, productosAdmin);
        var fecha = DateOnly.FromDateTime(propia.Fecha.AddHours(-5)).ToString("yyyy-MM-dd");
        var periodo = $"?desde={fecha}&hasta={fecha}";
        var antes = (await admin.GetFromJsonAsync<CierreCaja>($"/api/reportes/cierre-caja?fecha={fecha}"))!;
        Assert.Equal(2, antes.Ventas);
        Assert.Equal(54m, antes.Total);
        foreach (var ruta in new[] { "ventas-por-periodo", "ventas-por-vendedor", "top-productos",
            "margen-productos", "stock-bajo", "cierre-caja", "compras-por-proveedor" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/reportes/" + ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/reportes/" + ruta)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/reportes/mi-resumen")).StatusCode);
        var resumen = (await seller.GetFromJsonAsync<CierreCaja>($"/api/reportes/mi-resumen?fecha={fecha}&empleadoId=1"))!;
        Assert.Equal(1, resumen.Ventas);
        Assert.Equal(27m, resumen.Total);
        Assert.Single(resumen.PorVendedor);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/ventas/{propia.Id}/anulacion", new { motivo = "Revisión de reportes" })).StatusCode);
        var caja = (await admin.GetFromJsonAsync<CierreCaja>($"/api/reportes/cierre-caja?fecha={fecha}"))!;
        Assert.Equal(1, caja.Ventas);
        Assert.Equal(27m, caja.Total);
        Assert.Equal(1, caja.Anuladas);
        Assert.Equal(27m, caja.MontoAnulado);
        var personal = (await seller.GetFromJsonAsync<CierreCaja>($"/api/reportes/mi-resumen?fecha={fecha}&empleadoId=1"))!;
        Assert.Equal(0, personal.Ventas);
        Assert.Equal(0m, personal.Total);
        Assert.Equal(1, personal.Anuladas);
        Assert.Empty(personal.PorVendedor);
        foreach (var agrupacion in new[] { "Dia", "Mes" })
        {
            var filas = (await admin.GetFromJsonAsync<List<VentasPorPeriodo>>("/api/reportes/ventas-por-periodo" + periodo + "&agrupacion=" + agrupacion))!;
            Assert.Equal(27m, Assert.Single(filas).Total);
            Assert.Equal(1, filas[0].Ventas);
        }
        var vendedores = (await admin.GetFromJsonAsync<List<VentasPorVendedor>>("/api/reportes/ventas-por-vendedor" + periodo))!;
        Assert.Equal(27m, Assert.Single(vendedores).Total);
        var top = (await admin.GetFromJsonAsync<List<TopProducto>>("/api/reportes/top-productos" + periodo))!;
        Assert.Equal(2, top.Count);
        Assert.All(top, fila => Assert.Contains(fila.ProductoId, productosAdmin));
        Assert.Equal(27m, top.Sum(fila => fila.Total));
        var margen = (await admin.GetFromJsonAsync<List<MargenProducto>>("/api/reportes/margen-productos" + periodo))!;
        Assert.Equal(27m, margen.Sum(fila => fila.Ingreso));
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/reportes/stock-bajo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/reportes/compras-por-proveedor" + periodo)).StatusCode);
    }

    [Fact]
    public async Task Reportes_ValidanPeriodosLimiteSinDevolverErroresInternos()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        foreach (var ruta in new[] {
            "ventas-por-periodo?desde=9999-12-30&hasta=9999-12-31",
            "cierre-caja?fecha=9999-12-31", "mi-resumen?fecha=9999-12-31",
            "ventas-por-periodo?desde=2026-11-27&hasta=2026-11-20",
            "ventas-por-periodo?desde=2024-01-01&hasta=2026-11-20",
            "ventas-por-periodo?agrupacion=invalida", "top-productos?top=0" })
        {
            using var response = await admin.GetAsync("/api/reportes/" + ruta);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/reportes/ventas-por-periodo?hasta=0001-01-01")).StatusCode);
    }
}
