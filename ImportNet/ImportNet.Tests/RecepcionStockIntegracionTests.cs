using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionStockIntegracionTests
{
    [Fact]
    public async Task Vendedor_ConsultaStockActualizadoPeroNoPuedeRecibir()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var vendedor = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, vendedor, "Vendedor");

        var datos = await Preparar(factory, admin);

        await ComprobarPersistencia(factory, datos, recibida: false);

        // La compra existe y es válida, pero el vendedor no puede recibirla.
        using var prohibida = await vendedor.PostAsync(
            $"/api/compras/{datos.Compra.Id}/recibir",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);

        await ComprobarPersistencia(factory, datos, recibida: false);

        var antes = await ConsultarStock(vendedor);

        Assert.Equal(3, antes.Count);
        Assert.Equal(
            7.5m,
            antes.Items.Single(x => x.ProductoId == datos.OllaId).Cantidad);

        Assert.Equal(
            0m,
            antes.Items.Single(x => x.ProductoId == datos.CajaId).Cantidad);

        var bajosAntes = await ConsultarStock(vendedor, stockBajo: true);

        Assert.Equal(2, bajosAntes.Count);
        Assert.Contains(
            bajosAntes.Items,
            x => x.ProductoId == datos.CajaId);

        // El administrador recibe las dos líneas de la compra.
        using var respuesta = await admin.PostAsync(
            $"/api/compras/{datos.Compra.Id}/recibir",
            null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var recibida = await respuesta.Content
            .ReadFromJsonAsync<CompraDto>();

        Assert.NotNull(recibida);
        Assert.Equal("Recibida", recibida!.Estado);
        Assert.Equal(140m, recibida.TotalPen);

        await ComprobarPersistencia(factory, datos, recibida: true);

        // El vendedor consulta las existencias resultantes.
        var despues = await ConsultarStock(vendedor);

        Assert.Equal(3, despues.Count);

        Assert.Equal(
            9.5m,
            despues.Items.Single(x => x.ProductoId == datos.OllaId).Cantidad);

        Assert.Equal(
            3m,
            despues.Items.Single(x => x.ProductoId == datos.CajaId).Cantidad);

        Assert.Equal(
            2m,
            despues.Items.Single(x => x.ProductoId == datos.SartenId).Cantidad);

        var bajosDespues = await ConsultarStock(
            vendedor,
            stockBajo: true);

        Assert.Equal(1, bajosDespues.Count);
        Assert.Equal(
            datos.SartenId,
            Assert.Single(bajosDespues.Items).ProductoId);

        // El stock no expone costos aunque la recepción los haya actualizado.
        using var consulta = await vendedor.GetAsync(
            "/api/inventario/stock");

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        using var documento = JsonDocument.Parse(
            await consulta.Content.ReadAsStringAsync());

        foreach (var item in documento.RootElement
            .GetProperty("items")
            .EnumerateArray())
        {
            Assert.False(item.TryGetProperty("costoPromedio", out _));
            Assert.False(item.TryGetProperty("costoImportacionUsd", out _));
            Assert.False(item.TryGetProperty("tipoCambioImportacion", out _));
        }
    }

    [Fact]
    public async Task FalloDeGuardado_NoDejaCambiosParcialesYPermiteReintentar()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(factory, admin, "Administrador");

        var datos = await Preparar(factory, admin);

        // Este trigger existe exclusivamente en la SQLite temporal del test.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.True(db.Database.IsSqlite());

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER "FallarEntradaCajaPrueba"
                AFTER INSERT ON "MovimientoInventario"
                WHEN NEW."ProductoId" = (
                    SELECT "Id"
                    FROM "Producto"
                    WHERE "Codigo" = 'CAJ-03'
                )
                BEGIN
                    SELECT RAISE(FAIL, 'FALLO_RECEPCION_PRUEBA');
                END;
                """);
        }

        using var fallida = await admin.PostAsync(
            $"/api/compras/{datos.Compra.Id}/recibir",
            null);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            fallida.StatusCode);

        Assert.Equal(
            "application/problem+json",
            fallida.Content.Headers.ContentType?.MediaType);

        var cuerpoError = await fallida.Content.ReadAsStringAsync();

        Assert.DoesNotContain("FALLO_RECEPCION_PRUEBA", cuerpoError);

        // Verifica nuevamente desde otro contexto de EF.
        await ComprobarPersistencia(factory, datos, recibida: false);

        var stockTrasFallo = await ConsultarStock(admin);

        Assert.Equal(
            7.5m,
            stockTrasFallo.Items
                .Single(x => x.ProductoId == datos.OllaId)
                .Cantidad);

        Assert.Equal(
            0m,
            stockTrasFallo.Items
                .Single(x => x.ProductoId == datos.CajaId)
                .Cantidad);

        // Retira el fallo simulado y repite la recepción.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.True(db.Database.IsSqlite());

            await db.Database.ExecuteSqlRawAsync(
                """DROP TRIGGER "FallarEntradaCajaPrueba";""");
        }

        using var reintento = await admin.PostAsync(
            $"/api/compras/{datos.Compra.Id}/recibir",
            null);

        Assert.Equal(HttpStatusCode.OK, reintento.StatusCode);

        await ComprobarPersistencia(factory, datos, recibida: true);
    }

    private sealed record DatosPrueba(
        CompraDto Compra,
        int OllaId,
        int SartenId,
        int CajaId);

    private static async Task<DatosPrueba> Preparar(
        SmokeFactory factory,
        HttpClient admin)
    {
        StockConsultaTests.DatosStock productos;
        int proveedorId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            productos = await StockConsultaTests.Preparar(db);

            var proveedor = new Proveedor
            {
                Nombre = "Proveedor integración",
                Pais = "China",
                Contacto = "Ventas"
            };

            db.Add(proveedor);

            var olla = await db.Set<Producto>()
                .SingleAsync(x => x.Id == productos.OllaId);

            olla.RegistrarCostoImportacion(7.50m, 3.90m);

            await db.SaveChangesAsync();

            proveedorId = proveedor.Id;
        }

        var compraDatos = new CompraDatos(
            proveedorId,
            DateTime.UtcNow,
            4m,
            0m,
            0m,
            [
                new LineaCompra(productos.CajaId, 3m, 5m),
                new LineaCompra(productos.OllaId, 2m, 10m)
            ]);

        using var respuesta = await admin.PostAsJsonAsync(
            "/api/compras",
            compraDatos);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var compra = await respuesta.Content
            .ReadFromJsonAsync<CompraDto>();

        Assert.NotNull(compra);
        Assert.Equal("Borrador", compra!.Estado);
        Assert.Equal(140m, compra.TotalPen);

        return new DatosPrueba(
            compra,
            productos.OllaId,
            productos.SartenId,
            productos.CajaId);
    }

    private static async Task<Pagina<StockProducto>> ConsultarStock(
        HttpClient cliente,
        bool stockBajo = false)
    {
        var ruta = stockBajo
            ? "/api/inventario/stock?stockBajo=true"
            : "/api/inventario/stock";

        using var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var resultado = await respuesta.Content
            .ReadFromJsonAsync<Pagina<StockProducto>>();

        Assert.NotNull(resultado);

        return resultado!;
    }

    private static async Task ComprobarPersistencia(
        SmokeFactory factory,
        DatosPrueba datos,
        bool recibida)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var compra = await db.Set<Compra>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == datos.Compra.Id);

        Assert.Equal(
            recibida ? EstadoCompra.Recibida : EstadoCompra.Borrador,
            compra.Estado);

        var olla = await db.Set<Producto>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == datos.OllaId);

        var caja = await db.Set<Producto>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == datos.CajaId);

        var sarten = await db.Set<Producto>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == datos.SartenId);

        Assert.Equal(recibida ? 24.2105m : 20m, olla.CostoPromedio);
        Assert.Equal(recibida ? 20m : 0m, caja.CostoPromedio);
        Assert.Equal(30m, sarten.CostoPromedio);

        // La recepción conserva precios de venta y referencias históricas.
        Assert.Equal(80m, olla.PrecioVenta);
        Assert.Equal(25m, caja.PrecioVenta);
        Assert.Equal(60m, sarten.PrecioVenta);

        Assert.Equal(
            7.50m,
            olla.CostoImportacionUsd.GetValueOrDefault());

        Assert.Equal(
            3.90m,
            olla.TipoCambioImportacion.GetValueOrDefault());

        var inventarios = await db.Set<Inventario>()
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(recibida ? 3 : 2, inventarios.Count);

        Assert.All(
            inventarios,
            inventario => Assert.Equal(
                Almacen.PrincipalId,
                inventario.AlmacenId));

        Assert.Equal(
            recibida ? 9.5m : 7.5m,
            inventarios.Single(x => x.ProductoId == datos.OllaId).Cantidad);

        Assert.Equal(
            2m,
            inventarios.Single(x => x.ProductoId == datos.SartenId).Cantidad);

        var inventarioCaja = inventarios.SingleOrDefault(
            x => x.ProductoId == datos.CajaId);

        if (recibida)
        {
            Assert.NotNull(inventarioCaja);
            Assert.Equal(3m, inventarioCaja!.Cantidad);
        }
        else
        {
            Assert.Null(inventarioCaja);
        }

        var movimientos = await db.Set<MovimientoInventario>()
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(recibida ? 2 : 0, movimientos.Count);

        Assert.All(movimientos, movimiento =>
        {
            Assert.Equal(
                datos.Compra.Id,
                movimiento.CompraId.GetValueOrDefault());

            Assert.Equal(
                datos.Compra.ProveedorId,
                movimiento.ProveedorId.GetValueOrDefault());

            Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
            Assert.Equal(Almacen.PrincipalId, movimiento.AlmacenId);
        });

        if (recibida)
        {
            Assert.Equal(
                2m,
                movimientos.Single(
                    x => x.ProductoId == datos.OllaId).Cantidad);

            Assert.Equal(
                3m,
                movimientos.Single(
                    x => x.ProductoId == datos.CajaId).Cantidad);
        }
    }
}