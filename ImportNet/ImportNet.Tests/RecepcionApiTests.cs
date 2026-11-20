using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionApiTests
{
    [Fact]
    public async Task Permisos_Anonimo401YVendedor403()
    {
        using var factory = new SmokeFactory();
        using var anonimo = factory.CreateClient();
        using var vendedor = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            vendedor,
            "Vendedor");

        using var respuestaAnonima =
            await anonimo.PostAsync(
                "/api/compras/1/recibir",
                null);

        using var respuestaVendedor =
            await vendedor.PostAsync(
                "/api/compras/1/recibir",
                null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            respuestaAnonima.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            respuestaVendedor.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Empty(
            await db.Set<MovimientoInventario>()
                .ToListAsync());
    }

    [Fact]
    public async Task Administrador_RecibeConDatosYAutorDelServidor()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            admin,
            "Administrador");

        var usuario = await admin.GetFromJsonAsync<UsuarioDto>(
            "/api/auth/me");

        Assert.NotNull(usuario);

        var compra = await CrearCompra(factory, admin);

        using var respuesta = await admin.PostAsJsonAsync(
            $"/api/compras/{compra.Id}/recibir",
            new
            {
                empleadoId = int.MaxValue,
                cantidad = 999m,
                costoPromedio = 0m,
                estado = "Borrador"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            respuesta.StatusCode);

        var recibida = await respuesta.Content
            .ReadFromJsonAsync<CompraDto>();

        Assert.NotNull(recibida);
        Assert.Equal("Recibida", recibida!.Estado);
        Assert.Equal(91m, recibida.TotalPen);
        Assert.Equal(2m, Assert.Single(recibida.Lineas).Cantidad);

        var swagger = await admin.GetStringAsync(
            "/swagger/v1/swagger.json");

        Assert.Contains(
            "/api/compras/{id}/recibir",
            swagger);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var inventario = await db.Set<Inventario>()
            .SingleAsync();

        var producto = await db.Set<Producto>()
            .SingleAsync();

        var movimiento = await db
            .Set<MovimientoInventario>()
            .SingleAsync();

        Assert.Equal(11m, inventario.Cantidad);
        Assert.Equal(24.6364m, producto.CostoPromedio);
        Assert.Equal(30m, producto.PrecioVenta);

        Assert.Equal(
            usuario!.EmpleadoId,
            movimiento.EmpleadoId);

        Assert.Equal(
            compra.Id,
            movimiento.CompraId.GetValueOrDefault());

        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
    }

    [Fact]
    public async Task Recibida_RechazaRepeticionEdicionYEliminacion()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            admin,
            "Administrador");

        var compra = await CrearCompra(factory, admin);

        using var primera = await admin.PostAsync(
            $"/api/compras/{compra.Id}/recibir",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            primera.StatusCode);

        using var segunda = await admin.PostAsync(
            $"/api/compras/{compra.Id}/recibir",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            segunda.StatusCode);

        Assert.Equal(
            "application/problem+json",
            segunda.Content.Headers.ContentType?.MediaType);

        using var edicion = await admin.PutAsJsonAsync(
            $"/api/compras/{compra.Id}",
            compra);

        Assert.Equal(
            HttpStatusCode.Conflict,
            edicion.StatusCode);

        using var eliminacion = await admin.DeleteAsync(
            $"/api/compras/{compra.Id}");

        Assert.Equal(
            HttpStatusCode.Conflict,
            eliminacion.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Equal(
            11m,
            (await db.Set<Inventario>()
                .SingleAsync()).Cantidad);

        Assert.Equal(
            24.6364m,
            (await db.Set<Producto>()
                .SingleAsync()).CostoPromedio);

        Assert.Single(
            await db.Set<MovimientoInventario>()
                .ToListAsync());
    }

    [Fact]
    public async Task Administrador_IdInvalido400EInexistente404()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(
            factory,
            admin,
            "Administrador");

        using var invalida = await admin.PostAsync(
            "/api/compras/0/recibir",
            null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalida.StatusCode);

        using var inexistente = await admin.PostAsync(
            "/api/compras/999999/recibir",
            null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            inexistente.StatusCode);

        Assert.Equal(
            "application/problem+json",
            inexistente.Content.Headers.ContentType?.MediaType);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Empty(
            await db.Set<MovimientoInventario>()
                .ToListAsync());
    }

    private static async Task<CompraDto> CrearCompra(
        SmokeFactory factory,
        HttpClient admin)
    {
        var datos = await ComprasApiTests.Preparar(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var producto = await db.Set<Producto>()
                .SingleAsync(
                    x => x.Id == datos.Lineas[0].ProductoId);

            producto.ActualizarCostoPromedioPorRecepcion(
                0m,
                9m,
                180m);

            await db.SaveChangesAsync();
        }

        using var respuesta = await admin.PostAsJsonAsync(
            "/api/compras",
            datos);

        Assert.Equal(
            HttpStatusCode.Created,
            respuesta.StatusCode);

        var compra = await respuesta.Content
            .ReadFromJsonAsync<CompraDto>();

        Assert.NotNull(compra);

        return compra!;
    }
}