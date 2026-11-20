using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class StockApiTests
{
    [Theory]
    [InlineData("Administrador")]
    [InlineData("Vendedor")]
    public async Task AmbosRoles_ConsultanStockSinCostos(
        string rol)
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            client,
            rol);

        var datos = await Preparar(factory);

        using var respuesta = await client.GetAsync(
            "/api/inventario/stock");

        Assert.Equal(
            HttpStatusCode.OK,
            respuesta.StatusCode);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());

        var raiz = documento.RootElement;
        var items = raiz.GetProperty("items");

        Assert.Equal(
            3,
            raiz.GetProperty("count").GetInt32());

        Assert.Equal(3, items.GetArrayLength());

        var olla = items.EnumerateArray().Single(
            x => x.GetProperty("productoId").GetInt32()
                == datos.OllaId);

        var caja = items.EnumerateArray().Single(
            x => x.GetProperty("productoId").GetInt32()
                == datos.CajaId);

        Assert.Equal(
            7.5m,
            olla.GetProperty("cantidad").GetDecimal());

        Assert.Equal(
            0m,
            caja.GetProperty("cantidad").GetDecimal());

        Assert.True(
            caja.GetProperty("stockBajo").GetBoolean());

        foreach (var item in items.EnumerateArray())
        {
            Assert.False(
                item.TryGetProperty(
                    "costoPromedio",
                    out _));

            Assert.False(
                item.TryGetProperty(
                    "costoImportacionUsd",
                    out _));

            Assert.False(
                item.TryGetProperty(
                    "tipoCambioImportacion",
                    out _));
        }
    }

    [Fact]
    public async Task Anonimo_Recibe401()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();

        factory.InitializeSchema();

        using var respuesta = await client.GetAsync(
            "/api/inventario/stock");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            respuesta.StatusCode);
    }

    [Fact]
    public async Task Api_AplicaBusquedaFiltroYPaginacion()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            client,
            "Administrador");

        var datos = await Preparar(factory);

        var texto = Uri.EscapeDataString(
            "  sAr-02  ");

        var buscado =
            await client.GetFromJsonAsync<Pagina<StockProducto>>(
                $"/api/inventario/stock?texto={texto}");

        Assert.NotNull(buscado);
        Assert.Equal(1, buscado!.Count);

        Assert.Equal(
            datos.SartenId,
            Assert.Single(buscado.Items).ProductoId);

        var bajos =
            await client.GetFromJsonAsync<Pagina<StockProducto>>(
                "/api/inventario/stock?stockBajo=true");

        Assert.NotNull(bajos);
        Assert.Equal(2, bajos!.Count);

        Assert.All(
            bajos.Items,
            item => Assert.True(item.StockBajo));

        var segunda =
            await client.GetFromJsonAsync<Pagina<StockProducto>>(
                "/api/inventario/stock?pagina=2&tamano=2");

        Assert.NotNull(segunda);
        Assert.Equal(3, segunda!.Count);
        Assert.Equal(2, segunda.PaginaActual);
        Assert.Equal(2, segunda.Tamano);

        Assert.Equal(
            datos.SartenId,
            Assert.Single(segunda.Items).ProductoId);

        var swagger = await client.GetStringAsync(
            "/swagger/v1/swagger.json");

        Assert.Contains(
            "/api/inventario/stock",
            swagger);
    }

    [Fact]
    public async Task ParametrosInvalidos_Devuelven400()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            client,
            "Administrador");

        var textoLargo = Uri.EscapeDataString(
            new string('a', 101));

        var rutas = new[]
        {
            "/api/inventario/stock?pagina=0",
            "/api/inventario/stock?pagina=1000001",
            "/api/inventario/stock?tamano=0",
            "/api/inventario/stock?tamano=101",
            $"/api/inventario/stock?texto={textoLargo}"
        };

        foreach (var ruta in rutas)
        {
            using var respuesta = await client.GetAsync(ruta);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                respuesta.StatusCode);

            Assert.Equal(
                "application/problem+json",
                respuesta.Content.Headers.ContentType?.MediaType);
        }
    }

    private static async Task<StockConsultaTests.DatosStock>
        Preparar(SmokeFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        return await StockConsultaTests.Preparar(db);
    }
}