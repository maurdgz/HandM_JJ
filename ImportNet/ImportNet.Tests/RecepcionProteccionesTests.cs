using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionProteccionesTests
{
    [Fact]
    public async Task BaseDeDatos_RechazaEntradasDuplicadas()
    {
        using var fixture = new ApplicationFixture();

        var datos = await ComprasTests.Datos(fixture);

        var compra = await fixture.Mediator.Send(
            new CrearCompraCommand(datos));

        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "protecciones@example.com",
            Email = "protecciones@example.com"
        };

        var empleado = new Empleado
        {
            UsuarioId = usuario.Id,
            Nombre = "Usuario",
            Apellidos = "Prueba",
            DocumentoIdentidad = "00000003",
            Cargo = "Administrador",
            Area = "Pruebas"
        };

        fixture.Db.AddRange(usuario, empleado);
        await fixture.Db.SaveChangesAsync();

        MovimientoInventario NuevaEntrada() => new()
        {
            ProductoId = datos.Lineas[0].ProductoId,
            AlmacenId = Almacen.PrincipalId,
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 2m,
            EmpleadoId = empleado.Id,
            CompraId = compra.Id
        };

        fixture.Db.Add(NuevaEntrada());
        await fixture.Db.SaveChangesAsync();

        fixture.Db.Add(NuevaEntrada());

        await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.Db.SaveChangesAsync());

        fixture.Db.ChangeTracker.Clear();

        Assert.Single(
            await fixture.Db.Set<MovimientoInventario>()
                .Where(x => x.CompraId == compra.Id)
                .ToListAsync());
    }
}