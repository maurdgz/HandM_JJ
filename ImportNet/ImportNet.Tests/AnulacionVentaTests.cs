using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class AnulacionVentaTests
{
    [Fact]
    public async Task Anular_ExigeAdministradorYReponeUnaSolaVez()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var seller = factory.CreateClient();
        using var anon = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, seller, "Vendedor");

        var usuario = (await admin.GetFromJsonAsync<UsuarioDto>(
            "/api/auth/me"))!;

        int ventaId;
        int productoId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var categoria = new Categoria("Prueba de anulación");
            db.Add(categoria);
            await db.SaveChangesAsync();

            var producto = new Producto(
                "ANULACION-1",
                "Producto de prueba",
                null,
                "Unidad",
                null,
                10m,
                null,
                categoria.Id,
                0m);

            db.Add(producto);
            await db.SaveChangesAsync();

            productoId = producto.Id;

            var venta = new Venta
            {
                Numero = 1,
                EmpleadoId = usuario.EmpleadoId,
                Subtotal = 20m,
                Descuento = 0m,
                Total = 20m
            };

            db.Add(venta);
            await db.SaveChangesAsync();

            ventaId = venta.Id;

            db.AddRange(
                new DetalleVenta(productoId, 2m, 10m, 0m)
                {
                    VentaId = ventaId
                },
                new Inventario
                {
                    ProductoId = productoId,
                    AlmacenId = Almacen.PrincipalId,
                    Cantidad = 3m
                },
                new MovimientoInventario
                {
                    ProductoId = productoId,
                    AlmacenId = Almacen.PrincipalId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = 2m,
                    EmpleadoId = usuario.EmpleadoId,
                    VentaId = ventaId
                });

            await db.SaveChangesAsync();
        }

        var ruta = $"/api/ventas/{ventaId}/anulacion";
        var motivo = new { motivo = "Devolución de prueba" };

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anon.PostAsJsonAsync(ruta, motivo)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await seller.PostAsJsonAsync(ruta, motivo)).StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync(
                ruta, new { motivo = "   " })).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.PostAsJsonAsync(ruta, motivo)).StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync(ruta, motivo)).StatusCode);

        using var checkScope = factory.Services.CreateScope();

        var context = checkScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var inventarioFinal = await context.Set<Inventario>()
            .SingleAsync(i =>
                i.ProductoId == productoId &&
                i.AlmacenId == Almacen.PrincipalId);

        Assert.Equal(5m, inventarioFinal.Cantidad);

        var entradas = await context.Set<MovimientoInventario>()
            .Where(m =>
                m.VentaId == ventaId &&
                m.Tipo == TipoMovimiento.Entrada)
            .ToListAsync();

        var entrada = Assert.Single(entradas);

        Assert.Equal(2m, entrada.Cantidad);
        Assert.Equal(usuario.EmpleadoId, entrada.EmpleadoId);

        Assert.Equal(
            1,
            await context.Set<MovimientoInventario>()
                .CountAsync(m =>
                    m.VentaId == ventaId &&
                    m.Tipo == TipoMovimiento.Salida));

        var ventaFinal = await context.Set<Venta>()
            .SingleAsync(v => v.Id == ventaId);

        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);
        Assert.Equal("Devolución de prueba", ventaFinal.MotivoAnulacion);
        Assert.NotNull(ventaFinal.FechaAnulacion);
    }
}
