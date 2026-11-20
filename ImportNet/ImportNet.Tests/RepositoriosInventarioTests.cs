using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImportNet.Tests;

public sealed class RepositoriosInventarioTests
{
    private static async Task<(
        int ProductoId,
        int CompraId,
        int EmpleadoId)> Preparar(ApplicationFixture fixture)
    {
        var datos = await ComprasTests.Datos(fixture);

        var compra = await fixture.Mediator.Send(
            new CrearCompraCommand(datos));

        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "inventario@example.com",
            Email = "inventario@example.com"
        };

        var empleado = new Empleado
        {
            UsuarioId = usuario.Id,
            Nombre = "Prueba",
            Apellidos = "Inventario",
            DocumentoIdentidad = "00000001",
            Cargo = "Administrador",
            Area = "Pruebas"
        };

        fixture.Db.AddRange(usuario, empleado);
        await fixture.Db.SaveChangesAsync();

        var resultado = (
            ProductoId: datos.Lineas[0].ProductoId,
            CompraId: compra.Id,
            EmpleadoId: empleado.Id);

        fixture.Db.ChangeTracker.Clear();

        return resultado;
    }

    [Fact]
    public async Task Transaccion_ConfirmaCambiosRelacionados()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture);

        Assert.False(
            await fixture.Uow.Movimientos.TieneEntradasDeCompra(
                datos.CompraId,
                CancellationToken.None));

        var resultado = await fixture.Uow.EjecutarEnTransaccion(
            async ct =>
            {
                var inventario =
                    await fixture.Uow.Inventarios.Obtener(
                        datos.ProductoId,
                        Almacen.PrincipalId,
                        ct)
                    ?? throw new NoEncontradoException(
                        "Inventario de prueba no encontrado.");

                var compra =
                    await fixture.Uow.Compras.Obtener(
                        datos.CompraId,
                        ct)
                    ?? throw new NoEncontradoException(
                        "Compra de prueba no encontrada.");

                inventario.Cantidad += 2m;
                compra.Recibir();

                fixture.Uow.Movimientos.Agregar(
                    new MovimientoInventario
                    {
                        ProductoId = datos.ProductoId,
                        AlmacenId = Almacen.PrincipalId,
                        Tipo = TipoMovimiento.Entrada,
                        Cantidad = 2m,
                        EmpleadoId = datos.EmpleadoId,
                        CompraId = datos.CompraId
                    });

                return datos.CompraId;
            },
            CancellationToken.None);

        Assert.Equal(datos.CompraId, resultado);

        fixture.Db.ChangeTracker.Clear();

        var stockGuardado =
            await fixture.Uow.Inventarios.Obtener(
                datos.ProductoId,
                Almacen.PrincipalId,
                CancellationToken.None);

        Assert.NotNull(stockGuardado);
        Assert.Equal(9m, stockGuardado!.Cantidad);

        var compraGuardada =
            await fixture.Uow.Compras.Obtener(
                datos.CompraId,
                CancellationToken.None);

        Assert.NotNull(compraGuardada);
        Assert.Equal(
            EstadoCompra.Recibida,
            compraGuardada!.Estado);

        var movimientos =
            await fixture.Uow.Movimientos.ListarPorCompra(
                datos.CompraId,
                CancellationToken.None);

        var movimiento = Assert.Single(movimientos);

        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(
            datos.CompraId,
            movimiento.CompraId.GetValueOrDefault());

        Assert.True(
            await fixture.Uow.Movimientos.TieneEntradasDeCompra(
                datos.CompraId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Transaccion_RevierteInclusoCambiosYaGuardados()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => fixture.Uow.EjecutarEnTransaccion<bool>(
                async ct =>
                {
                    var inventario =
                        await fixture.Uow.Inventarios.Obtener(
                            datos.ProductoId,
                            Almacen.PrincipalId,
                            ct)
                        ?? throw new NoEncontradoException(
                            "Inventario de prueba no encontrado.");

                    var compra =
                        await fixture.Uow.Compras.Obtener(
                            datos.CompraId,
                            ct)
                        ?? throw new NoEncontradoException(
                            "Compra de prueba no encontrada.");

                    inventario.Cantidad += 2m;
                    compra.Recibir();

                    fixture.Uow.Movimientos.Agregar(
                        new MovimientoInventario
                        {
                            ProductoId = datos.ProductoId,
                            AlmacenId = Almacen.PrincipalId,
                            Tipo = TipoMovimiento.Entrada,
                            Cantidad = 2m,
                            EmpleadoId = datos.EmpleadoId,
                            CompraId = datos.CompraId
                        });

                    await fixture.Uow.Guardar(ct);

                    throw new ReglaNegocioException(
                        "Fallo simulado después de guardar.");
                },
                CancellationToken.None));

        Assert.Empty(fixture.Db.ChangeTracker.Entries());

        var stockGuardado =
            await fixture.Uow.Inventarios.Obtener(
                datos.ProductoId,
                Almacen.PrincipalId,
                CancellationToken.None);

        Assert.NotNull(stockGuardado);
        Assert.Equal(7m, stockGuardado!.Cantidad);

        var compraGuardada =
            await fixture.Uow.Compras.Obtener(
                datos.CompraId,
                CancellationToken.None);

        Assert.NotNull(compraGuardada);
        Assert.Equal(
            EstadoCompra.Borrador,
            compraGuardada!.Estado);

        Assert.Empty(
            await fixture.Uow.Movimientos.ListarPorCompra(
                datos.CompraId,
                CancellationToken.None));

        Assert.False(
            await fixture.Uow.Movimientos.TieneEntradasDeCompra(
                datos.CompraId,
                CancellationToken.None));
    }
}