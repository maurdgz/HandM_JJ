using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class ReportesExcelTests
{
    private const string Fecha = "2026-11-20";
    private const string Rango = $"desde={Fecha}&hasta={Fecha}";

    [Fact]
    public async Task Excel_UsaLasMismasFilasColumnasYTotalesDeLaTabla()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        await Sembrar(factory, admin);

        var casos = new (string Reporte, string Filtros, decimal? TotalEsperado, int ColumnaTotal)[]
        {
            ("ventas-por-periodo", Rango + "&agrupacion=Mes", 125.50m, 2),
            ("ventas-por-vendedor", Rango, 125.50m, 2),
            ("top-productos", Rango + "&top=1", 90m, 4),
            ("margen-productos", Rango, 125.50m, 3),
            ("stock-bajo", "", null, 0),
            ("cierre-caja", "fecha=" + Fecha, 125.50m, 4),
            ("compras-por-proveedor", Rango, 0m, 4)
        };

        foreach (var (reporte, filtros, totalEsperado, columnaTotal) in casos)
        {
            var tabla = await admin.GetFromJsonAsync<Tabla>($"/api/reportes/{reporte}/tabla?{filtros}");
            Assert.NotNull(tabla);
            if (totalEsperado.HasValue)
                Assert.Equal(totalEsperado.Value, tabla.Totales[columnaTotal].GetDecimal());
            if (reporte == "cierre-caja")
            {
                Assert.Equal(1, tabla.Totales[5].GetInt32());
                Assert.Equal(75m, tabla.Totales[6].GetDecimal());
            }

            using var respuesta = await admin.GetAsync($"/api/reportes/{reporte}/excel?{filtros}");
            respuesta.EnsureSuccessStatusCode();
            Assert.True(respuesta.Headers.CacheControl!.NoStore);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                respuesta.Content.Headers.ContentType?.MediaType);
            var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar;
            Assert.Equal(tabla.NombreArchivo + ".xlsx", nombre);
            if (filtros != "")
            {
                Assert.Contains(Fecha, nombre);
                Assert.Contains(Fecha, tabla.Filtros);
            }
            if (reporte == "ventas-por-periodo")
            {
                Assert.EndsWith("-mes.xlsx", nombre);
                Assert.Contains("Agrupación por mes", tabla.Filtros);
                Assert.Equal("2026-11", Assert.Single(tabla.Filas)[0].GetString());
            }
            if (reporte == "top-productos")
            {
                Assert.EndsWith("-top-1.xlsx", nombre);
                Assert.Contains("Máximo 1", tabla.Filtros);
                Assert.Single(tabla.Filas);
            }

            using var contenido = new MemoryStream(await respuesta.Content.ReadAsByteArrayAsync());
            using var libro = new XLWorkbook(contenido);
            var hoja = libro.Worksheet("Reporte");
            Assert.Equal($"{tabla.Titulo} | {tabla.Filtros}", libro.Properties.Title);
            Assert.Equal(libro.Properties.Title, hoja.Cell("A1").GetString());
            var filaTotales = tabla.Filas.Length + 4;
            Assert.Equal(filaTotales, hoja.LastRowUsed()!.RowNumber());

            for (var i = 0; i < tabla.Columnas.Length; i++)
            {
                var columna = tabla.Columnas[i];
                Assert.Equal(columna.Titulo, hoja.Cell(3, i + 1).GetString());
                for (var fila = 0; fila < tabla.Filas.Length; fila++)
                    VerificarCelda(hoja.Cell(fila + 4, i + 1), tabla.Filas[fila][i], columna.Formato);
                VerificarCelda(hoja.Cell(filaTotales, i + 1), tabla.Totales[i], columna.Formato);
            }
        }
    }

    [Theory]
    [InlineData("tabla")]
    [InlineData("excel")]
    public async Task Reporte_ExigeAdminYValidaLosMismosFiltros(string formato)
    {
        using var factory = new SmokeFactory();
        using var anonimo = factory.CreateClient();
        using var vendedor = factory.CreateClient();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, vendedor, "Vendedor");
        await SmokeTests.Login(factory, admin, "Administrador");

        foreach (var reporte in new[] { "ventas-por-periodo", "ventas-por-vendedor", "top-productos",
                     "margen-productos", "stock-bajo", "cierre-caja", "compras-por-proveedor", "mi-resumen" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/reportes/{reporte}/{formato}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.GetAsync($"/api/reportes/{reporte}/{formato}")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/reportes/desconocido/{formato}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync($"/api/reportes/ventas-por-periodo/{formato}?desde=2026-11-21&hasta={Fecha}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync($"/api/reportes/ventas-por-periodo/{formato}?agrupacion=semana")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync($"/api/reportes/top-productos/{formato}?top=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync($"/api/reportes/cierre-caja/{formato}?fecha=9999-12-31")).StatusCode);
    }

    internal static async Task Sembrar(SmokeFactory factory, HttpClient admin)
    {
        var me = (await admin.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Categoria("Hogar");
        db.Add(categoria);
        await db.SaveChangesAsync();

        var olla = new Producto("001", "=Olla", null, "Unidad", null, 45, null, categoria.Id, 3);
        var sarten = new Producto("002", "Sartén", null, "Unidad", null, 35.50m, null, categoria.Id, 2);
        olla.ActualizarCostoPromedioPorRecepcion(0, 5, 114.26m);
        db.AddRange(olla, sarten);
        await db.SaveChangesAsync();

        var dia = new DateTime(2026, 11, 20, 5, 0, 0, DateTimeKind.Utc);
        var venta = new Venta(1, me.EmpleadoId, 1, 125.50m, 0, dia);
        var anulada = new Venta(2, me.EmpleadoId, 1, 75m, 0, dia);
        anulada.Anular("Prueba de reportes");
        var fueraDelRango = new Venta(3, me.EmpleadoId, 1, 50m, 0, dia.AddSeconds(-1));
        db.AddRange(venta, anulada, fueraDelRango);
        await db.SaveChangesAsync();
        db.AddRange(new DetalleVenta(olla.Id, 2, 45, 0) { VentaId = venta.Id },
            new DetalleVenta(sarten.Id, 1, 35.50m, 0) { VentaId = venta.Id });
        await db.SaveChangesAsync();
    }

    private static void VerificarCelda(IXLCell celda, JsonElement valor, string formato)
    {
        Assert.False(celda.HasFormula);
        switch (valor.ValueKind)
        {
            case JsonValueKind.Null:
                Assert.True(celda.IsEmpty());
                break;
            case JsonValueKind.String:
                Assert.Equal(XLDataType.Text, celda.DataType);
                Assert.Equal(valor.GetString(), celda.GetString());
                break;
            case JsonValueKind.Number:
                Assert.Equal(XLDataType.Number, celda.DataType);
                Assert.Equal((double)valor.GetDecimal(), celda.GetDouble(), 8);
                if (formato is "pen" or "pen4") Assert.Contains("S/", celda.Style.NumberFormat.Format);
                if (formato == "usd") Assert.Contains("USD", celda.Style.NumberFormat.Format);
                if (formato == "porcentaje") Assert.Contains("%", celda.Style.NumberFormat.Format);
                break;
            default:
                throw new InvalidOperationException("Valor inesperado.");
        }
    }

    private sealed record Columna(string Titulo, string Formato);
    private sealed record Tabla(string Titulo, string Filtros, string NombreArchivo, Columna[] Columnas,
        JsonElement[][] Filas, JsonElement[] Totales);
}
