using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Application.UseCases.Inventario;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class AjusteInventarioDominioTests
{
    private static MovimientoInventario Crear(MotivoAjuste motivo, decimal cantidad, decimal stock)
        => AjusteInventario.Crear(1, motivo, cantidad, "  nota  ", stock, 1, DateTime.UtcNow);

    [Theory]
    [InlineData(MotivoAjuste.Merma)]
    [InlineData(MotivoAjuste.Rotura)]
    [InlineData(MotivoAjuste.DevolucionProveedor)]
    public void MotivosDeSalida_RestanLaCantidadIndicada(MotivoAjuste motivo)
    {
        var movimiento = Crear(motivo, 2.5m, 10m);

        Assert.Equal(TipoMovimiento.Ajuste, movimiento.Tipo);
        Assert.Equal(-2.5m, movimiento.Cantidad);
        Assert.Equal(-2.5m, movimiento.CantidadConSigno);
        Assert.Equal(motivo, movimiento.MotivoAjuste);
        Assert.Equal("nota", movimiento.Observacion);
        Assert.Equal(Almacen.PrincipalId, movimiento.AlmacenId);
    }

    [Theory]
    [InlineData(12, 10, 2)]
    [InlineData(7, 10, -3)]
    [InlineData(0, 4, -4)]
    public void ConteoFisico_RegistraLaDiferenciaConElStock(decimal contado, decimal stock, decimal diferencia)
    {
        Assert.Equal(diferencia, Crear(MotivoAjuste.ConteoFisico, contado, stock).Cantidad);
    }

    [Fact]
    public void AjusteQueDejariaStockNegativo_SeRechaza()
    {
        var error = Assert.Throws<ReglaNegocioException>(
            () => Crear(MotivoAjuste.Merma, 3m, 2m));

        Assert.Contains("negativo", error.Message);
    }

    [Fact]
    public void ConteoIgualAlStock_NoGeneraAjuste()
    {
        Assert.Throws<ReglaNegocioException>(
            () => Crear(MotivoAjuste.ConteoFisico, 5m, 5m));
    }

    [Theory]
    [InlineData(MotivoAjuste.Merma, 0)]
    [InlineData(MotivoAjuste.Merma, -1)]
    [InlineData(MotivoAjuste.Rotura, 0.0001)]
    [InlineData(MotivoAjuste.ConteoFisico, -1)]
    [InlineData(MotivoAjuste.Merma, 1000001)]
    public void CantidadInvalida_SeRechaza(MotivoAjuste motivo, decimal cantidad)
    {
        Assert.Throws<ReglaNegocioException>(() => Crear(motivo, cantidad, 2000000m));
    }

    [Fact]
    public void ObservacionDemasiadoLarga_SeRechaza()
    {
        Assert.Throws<ReglaNegocioException>(() => AjusteInventario.Crear(
            1, MotivoAjuste.Merma, 1m, new string('x', 301), 5m, 1, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("merma", MotivoAjuste.Merma)]
    [InlineData("ConteoFisico", MotivoAjuste.ConteoFisico)]
    [InlineData(" devolucionproveedor ", MotivoAjuste.DevolucionProveedor)]
    public void Interpretar_AceptaNombresSinDistinguirMayusculas(string texto, MotivoAjuste esperado)
    {
        Assert.Equal(esperado, AjusteInventario.Interpretar(texto));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2")]
    [InlineData("Robo")]
    [InlineData(null)]
    public void Interpretar_RechazaMotivosDesconocidosONumericos(string? texto)
    {
        Assert.Throws<ReglaNegocioException>(() => AjusteInventario.Interpretar(texto));
    }
}

public sealed class AjustesKardexApiTests
{
    [Fact]
    public async Task Flujo_CompraVentaAjustes_KardexReproduceElStockActual()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");

        var productoId = await PrepararProductoConCompraRecibida(factory, admin, cantidad: 10m);

        using (var venta = await admin.PostAsJsonAsync("/api/ventas", new
        {
            clienteId = (int?)null,
            lineas = new[] { new { productoId, cantidad = 3m, descuento = 0m } }
        }))
            Assert.Equal(HttpStatusCode.Created, venta.StatusCode);

        var merma = await Ajustar(admin, productoId, "Merma", 2m, "Abolladas en el almacén");
        Assert.Equal(-2m, merma.Diferencia);
        Assert.Equal(7m, merma.StockAnterior);
        Assert.Equal(5m, merma.StockNuevo);
        Assert.True(merma.MovimientoId > 0);

        var conteo = await Ajustar(admin, productoId, "ConteoFisico", 6m, null);
        Assert.Equal(1m, conteo.Diferencia);
        Assert.Equal(6m, conteo.StockNuevo);

        // Rechazado: dejaría el stock en -1. No debe quedar ningún rastro.
        using (var negativo = await admin.PostAsJsonAsync("/api/inventario/ajustes",
            new { productoId, motivo = "Rotura", cantidad = 7m }))
            Assert.Equal(HttpStatusCode.BadRequest, negativo.StatusCode);

        var kardex = await ConsultarKardex(admin, productoId);

        Assert.Equal(0m, kardex.SaldoInicial);
        Assert.Equal(11m, kardex.TotalEntradas);
        Assert.Equal(5m, kardex.TotalSalidas);
        Assert.Equal(6m, kardex.SaldoFinal);
        Assert.Equal(kardex.StockActual, kardex.SaldoFinal);

        Assert.Collection(kardex.Movimientos,
            x => { Assert.StartsWith("Compra #", x.Referencia); Assert.Equal(10m, x.Entrada); Assert.Equal(10m, x.Saldo); },
            x => { Assert.Equal("Venta N° 1", x.Referencia); Assert.Equal(3m, x.Salida); Assert.Equal(7m, x.Saldo); },
            x =>
            {
                Assert.Equal("Ajuste: Merma", x.Referencia);
                Assert.Equal("Abolladas en el almacén", x.Observacion);
                Assert.Equal(2m, x.Salida);
                Assert.Equal(5m, x.Saldo);
            },
            x => { Assert.Equal("Ajuste: Conteo físico", x.Referencia); Assert.Equal(1m, x.Entrada); Assert.Equal(6m, x.Saldo); });

        Assert.All(kardex.Movimientos, x => Assert.False(string.IsNullOrWhiteSpace(x.Empleado)));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(4, await db.Set<MovimientoInventario>().CountAsync(x => x.ProductoId == productoId));
        Assert.Equal(6m, (await db.Set<Inventario>().SingleAsync(x => x.ProductoId == productoId)).Cantidad);
    }

    [Fact]
    public async Task FiltroDeFechas_CalculaSaldoInicialYValidaElRango()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");

        var productoId = await PrepararProductoConCompraRecibida(factory, admin, cantidad: 4m);
        await Ajustar(admin, productoId, "Rotura", 1m, null);

        var hoyPeru = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

        var hoy = await ConsultarKardex(admin, productoId, $"?desde={hoyPeru:yyyy-MM-dd}&hasta={hoyPeru:yyyy-MM-dd}");
        Assert.Equal(0m, hoy.SaldoInicial);
        Assert.Equal(2, hoy.Movimientos.Count);
        Assert.Equal(3m, hoy.SaldoFinal);

        var manana = await ConsultarKardex(admin, productoId, $"?desde={hoyPeru.AddDays(1):yyyy-MM-dd}");
        Assert.Equal(3m, manana.SaldoInicial);
        Assert.Empty(manana.Movimientos);
        Assert.Equal(3m, manana.SaldoFinal);

        var ayer = await ConsultarKardex(admin, productoId, $"?hasta={hoyPeru.AddDays(-1):yyyy-MM-dd}");
        Assert.Empty(ayer.Movimientos);
        Assert.Equal(0m, ayer.SaldoFinal);
        Assert.Equal(3m, ayer.StockActual);

        using var invertido = await admin.GetAsync(
            $"/api/inventario/kardex/{productoId}?desde={hoyPeru:yyyy-MM-dd}&hasta={hoyPeru.AddDays(-1):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);

        using var inexistente = await admin.GetAsync("/api/inventario/kardex/99999");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact]
    public async Task Vendedor_YAnonimo_NoRegistranAjustesNiVenKardex()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var vendedor = factory.CreateClient();
        using var anonimo = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, vendedor, "Vendedor");

        var productoId = await PrepararProductoConCompraRecibida(factory, admin, cantidad: 5m);
        var ajuste = new { productoId, motivo = "Merma", cantidad = 1m };

        using (var r = await vendedor.PostAsJsonAsync("/api/inventario/ajustes", ajuste))
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        using (var r = await vendedor.GetAsync($"/api/inventario/kardex/{productoId}"))
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        using (var r = await anonimo.PostAsJsonAsync("/api/inventario/ajustes", ajuste))
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        using (var r = await anonimo.GetAsync($"/api/inventario/kardex/{productoId}"))
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);

        // El vendedor sigue consultando stock, que no cambió.
        using (var r = await vendedor.GetAsync("/api/inventario/stock"))
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(5m, (await db.Set<Inventario>().SingleAsync(x => x.ProductoId == productoId)).Cantidad);
        Assert.False(await db.Set<MovimientoInventario>().AnyAsync(x => x.Tipo == TipoMovimiento.Ajuste));
    }

    [Theory]
    [InlineData("Robo", 1)]
    [InlineData("Merma", 0)]
    [InlineData("Merma", 1.0001)]
    public async Task AjusteInvalido_Devuelve400(string motivo, decimal cantidad)
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        var productoId = await PrepararProductoConCompraRecibida(factory, admin, cantidad: 5m);

        using var r = await admin.PostAsJsonAsync("/api/inventario/ajustes", new { productoId, motivo, cantidad });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Movimientos_NoSePuedenModificarNiEliminar()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        var productoId = await PrepararProductoConCompraRecibida(factory, admin, cantidad: 5m);
        var ajuste = await Ajustar(admin, productoId, "Merma", 1m, null);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var movimiento = await db.Set<MovimientoInventario>().SingleAsync(x => x.Id == ajuste.MovimientoId);
            movimiento.Cantidad = -5m;
            await Assert.ThrowsAsync<ReglaNegocioException>(() => db.SaveChangesAsync());
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Remove(await db.Set<MovimientoInventario>().SingleAsync(x => x.Id == ajuste.MovimientoId));
            await Assert.ThrowsAsync<ReglaNegocioException>(() => db.SaveChangesAsync());
        }

        var kardex = await ConsultarKardex(admin, productoId);
        Assert.Equal(2, kardex.Movimientos.Count);
        Assert.Equal(1m, kardex.Movimientos[1].Salida);
        Assert.Equal(4m, kardex.SaldoFinal);
        Assert.Equal(kardex.StockActual, kardex.SaldoFinal);
    }

    private static async Task<int> PrepararProductoConCompraRecibida(
        SmokeFactory factory, HttpClient admin, decimal cantidad)
    {
        int productoId, proveedorId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoria = new Categoria("Menaje kardex");
            db.Add(categoria);
            await db.SaveChangesAsync();

            var producto = new Producto("OLL-K1", "Olla kardex", null, "Unidad", null, 50m, null, categoria.Id, 1m);
            var proveedor = new Proveedor { Nombre = "Proveedor kardex", Pais = "China", Contacto = "Ventas" };
            db.AddRange(producto, proveedor);
            await db.SaveChangesAsync();

            productoId = producto.Id;
            proveedorId = proveedor.Id;
        }

        using var creada = await admin.PostAsJsonAsync("/api/compras", new CompraDatos(
            proveedorId, DateTime.UtcNow, 3.8m, 0m, 0m, [new LineaCompra(productoId, cantidad, 5m)]));
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var compra = (await creada.Content.ReadFromJsonAsync<CompraDto>())!;

        using var recibida = await admin.PostAsync($"/api/compras/{compra.Id}/recibir", null);
        Assert.Equal(HttpStatusCode.OK, recibida.StatusCode);

        return productoId;
    }

    private static async Task<AjusteRegistradoDto> Ajustar(
        HttpClient admin, int productoId, string motivo, decimal cantidad, string? observacion)
    {
        using var r = await admin.PostAsJsonAsync("/api/inventario/ajustes",
            new RegistrarAjusteCommand(productoId, motivo, cantidad, observacion));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<AjusteRegistradoDto>())!;
    }

    private static async Task<KardexProducto> ConsultarKardex(
        HttpClient admin, int productoId, string filtros = "")
    {
        using var r = await admin.GetAsync($"/api/inventario/kardex/{productoId}{filtros}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<KardexProducto>())!;
    }
}
