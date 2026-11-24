using ImportNet.Application;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionCompraTests
{
    [Fact]
    public async Task Recibir_ActualizaStockCostoYAutorDelMovimiento()
    {
        using var fixture = new Fixture();
        var datos = await Preparar(fixture);

        var resultado = await fixture.Mediator.Send(
            new RecibirCompraCommand(datos.CompraId));

        Assert.Equal("Recibida", resultado.Estado);
        Assert.Equal(90m, resultado.TotalPen);

        fixture.Db.ChangeTracker.Clear();

        var inventario = await fixture.Db
            .Set<Inventario>()
            .SingleAsync();

        var producto = await fixture.Db
            .Set<Producto>()
            .SingleAsync();

        var movimiento = await fixture.Db
            .Set<MovimientoInventario>()
            .SingleAsync();

        Assert.Equal(9m, inventario.Cantidad);
        Assert.Equal(25.5556m, producto.CostoPromedio);
        Assert.Equal(100m, producto.PrecioVenta);

        Assert.Equal(
            8m,
            producto.CostoImportacionUsd.GetValueOrDefault());

        Assert.Equal(
            3.5m,
            producto.TipoCambioImportacion.GetValueOrDefault());

        Assert.Equal(2m, movimiento.Cantidad);
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(datos.EmpleadoId, movimiento.EmpleadoId);

        Assert.Equal(
            datos.CompraId,
            movimiento.CompraId.GetValueOrDefault());

        Assert.Equal(
            Almacen.PrincipalId,
            movimiento.AlmacenId);
    }

    [Fact]
    public async Task Recibir_CreaInventarioCuandoNoExiste()
    {
        using var fixture = new Fixture();
        var datos = await Preparar(
            fixture,
            conStock: false);

        await fixture.Mediator.Send(
            new RecibirCompraCommand(datos.CompraId));

        fixture.Db.ChangeTracker.Clear();

        var inventario = await fixture.Db
            .Set<Inventario>()
            .SingleAsync();

        var producto = await fixture.Db
            .Set<Producto>()
            .SingleAsync();

        Assert.Equal(2m, inventario.Cantidad);
        Assert.Equal(45m, producto.CostoPromedio);
    }

    [Fact]
    public async Task RecibirDosVeces_RechazaSinDuplicarEntradas()
    {
        using var fixture = new Fixture();
        var datos = await Preparar(fixture);

        await fixture.Mediator.Send(
            new RecibirCompraCommand(datos.CompraId));

        await Assert.ThrowsAsync<ConflictoException>(
            () => fixture.Mediator.Send(
                new RecibirCompraCommand(datos.CompraId)));

        fixture.Db.ChangeTracker.Clear();

        Assert.Equal(
            9m,
            (await fixture.Db.Set<Inventario>()
                .SingleAsync()).Cantidad);

        Assert.Equal(
            25.5556m,
            (await fixture.Db.Set<Producto>()
                .SingleAsync()).CostoPromedio);

        Assert.Single(
            await fixture.Db.Set<MovimientoInventario>()
                .ToListAsync());
    }

    [Fact]
    public async Task Vendedor_NoPuedeRecibirCompras()
    {
        using var fixture = new Fixture();
        var datos = await Preparar(fixture);

        fixture.Actor.Administrador = false;

        var error =
            await Assert.ThrowsAsync<AccesoNoPermitidoException>(
                () => fixture.Mediator.Send(
                    new RecibirCompraCommand(datos.CompraId)));

        Assert.Equal(403, error.Status);

        fixture.Db.ChangeTracker.Clear();

        Assert.Equal(
            7m,
            (await fixture.Db.Set<Inventario>()
                .SingleAsync()).Cantidad);

        Assert.Equal(
            EstadoCompra.Borrador,
            (await fixture.Db.Set<Compra>()
                .SingleAsync()).Estado);

        Assert.Empty(
            await fixture.Db.Set<MovimientoInventario>()
                .ToListAsync());
    }

    private static async Task<DatosPrueba> Preparar(
        Fixture fixture,
        bool conStock = true)
    {
        var categoria = new Categoria("Menaje");

        var proveedor = new Proveedor
        {
            Nombre = "Proveedor de prueba",
            Pais = "China",
            Contacto = "Pruebas"
        };

        var usuario = new ApplicationUser
        {
            Id = fixture.Actor.Id!,
            UserName = "recepcion@example.com",
            Email = "recepcion@example.com"
        };

        fixture.Db.AddRange(
            categoria,
            proveedor,
            usuario);

        await fixture.Db.SaveChangesAsync();

        var producto = new Producto(
            "OLLA-RECEPCION",
            "Olla de prueba",
            null,
            "Unidad",
            null,
            100m,
            null,
            categoria.Id,
            2m);

        producto.RegistrarCostoImportacion(
            8m,
            3.5m);

        if (conStock)
        {
            producto.ActualizarCostoPromedioPorRecepcion(
                0m,
                7m,
                140m);
        }

        var empleado = new Empleado
        {
            UsuarioId = usuario.Id,
            Nombre = "Usuario",
            Apellidos = "Prueba",
            DocumentoIdentidad = "00000002",
            Cargo = "Administrador",
            Area = "Pruebas"
        };

        fixture.Db.AddRange(producto, empleado);
        await fixture.Db.SaveChangesAsync();

        var compra = new Compra(
            proveedor.Id,
            DateTime.UtcNow,
            3.75m,
            10m,
            5m,
            [new LineaCompra(producto.Id, 2m, 10m)]);

        fixture.Db.Add(compra);

        if (conStock)
        {
            fixture.Db.Add(new Inventario
            {
                ProductoId = producto.Id,
                AlmacenId = Almacen.PrincipalId,
                Cantidad = 7m
            });
        }

        await fixture.Db.SaveChangesAsync();

        var datos = new DatosPrueba(
            compra.Id,
            producto.Id,
            empleado.Id);

        fixture.Db.ChangeTracker.Clear();

        return datos;
    }

    private sealed record DatosPrueba(
        int CompraId,
        int ProductoId,
        int EmpleadoId);

    private sealed class ActorPrueba : IUsuarioActual
    {
        public string? Id { get; } = "usuario-recepcion";
        public bool Administrador { get; set; } = true;

        public Task ExigirAdministrador(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Id is null)
                throw new AccesoNoPermitidoException(401);

            if (!Administrador)
                throw new AccesoNoPermitidoException(403);

            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection =
            new("Data Source=:memory:");

        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public ActorPrueba Actor { get; } = new();

        public AppDbContext Db =>
            _scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        public IMediator Mediator =>
            _scope.ServiceProvider
                .GetRequiredService<IMediator>();

        public Fixture()
        {
            _connection.Open();

            var config =
                new ConfigurationBuilder().Build();

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IConfiguration>(config);

            services.AddApplication()
                .AddInfrastructure(config);

            services.AddSingleton<IUsuarioActual>(Actor);

            TestSupport.UseTestDatabase(
                services,
                _connection);

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();

            TestSupport.CreateTestSchema(
                _scope.ServiceProvider);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
        }
    }
}