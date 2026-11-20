using System.Data.Common;
using ImportNet.Application;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionConcurrenciaPostgreSqlTests
{
    [PostgreSqlTheory]
    [Trait("Database", "PostgreSQL")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecepcionesSimultaneas_ConservanStockYCostos(
        bool comprasDiferentes)
    {
        using var escenario = new Escenario();

        var datos = await escenario.Preparar(
            comprasDiferentes);

        var resultados = await Task.WhenAll(
            escenario.Recibir(datos.Primera),
            escenario.Recibir(datos.Segunda));

        Assert.True(escenario.LecturasSincronizadas);

        Assert.Single(
            resultados.Where(x => x.Exito));

        var rechazado = Assert.Single(
            resultados.Where(x => !x.Exito));

        if (comprasDiferentes)
        {
            var reintento = await escenario.Recibir(
                rechazado.CompraId);

            Assert.True(reintento.Exito);
        }

        using var scope = escenario.CrearScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var inventario = await db.Set<Inventario>()
            .SingleAsync();

        var producto = await db.Set<Producto>()
            .SingleAsync();

        var movimientos = await db
            .Set<MovimientoInventario>()
            .ToListAsync();

        var compras = await db.Set<Compra>()
            .ToListAsync();

        Assert.Equal(
            comprasDiferentes ? 12m : 9m,
            inventario.Cantidad);

        Assert.Equal(
            comprasDiferentes ? 24.1667m : 25.5556m,
            producto.CostoPromedio);

        Assert.Equal(
            comprasDiferentes ? 2 : 1,
            movimientos.Count);

        Assert.All(
            compras,
            compra => Assert.Equal(
                EstadoCompra.Recibida,
                compra.Estado));

        Assert.All(
            movimientos,
            movimiento =>
            {
                Assert.Equal(
                    TipoMovimiento.Entrada,
                    movimiento.Tipo);

                Assert.NotNull(movimiento.CompraId);
            });
    }

    private sealed record Resultado(
        int CompraId,
        bool Exito);

    private sealed class ActorPrueba : IUsuarioActual
    {
        public string? Id => "usuario-concurrencia";

        public Task ExigirAdministrador(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class BarreraLecturas
        : DbCommandInterceptor
    {
        private readonly TaskCompletionSource<bool> _listas =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _llegadas;

        public bool Completada =>
            Volatile.Read(ref _llegadas) >= 2;

        public override async ValueTask<DbDataReader>
            ReaderExecutedAsync(
                DbCommand command,
                CommandExecutedEventData eventData,
                DbDataReader result,
                CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                "FROM \"Compra\"",
                StringComparison.Ordinal))
            {
                if (Interlocked.Increment(ref _llegadas) == 2)
                    _listas.TrySetResult(true);

                await _listas.Task.WaitAsync(
                    TimeSpan.FromSeconds(20),
                    cancellationToken);
            }

            return result;
        }
    }

    private sealed class Escenario : IDisposable
    {
        private readonly string _esquema =
            "importnet_prueba_" + Guid.NewGuid().ToString("N");

        private readonly NpgsqlConnection _administrador;
        private readonly BarreraLecturas _barrera = new();

        private ServiceProvider? _provider;
        private bool _esquemaCreado;

        public bool LecturasSincronizadas =>
            _barrera.Completada;

        public Escenario()
        {
            var configuracion = new ConfigurationBuilder()
                .AddUserSecrets("ImportNet-local-2026")
                .AddEnvironmentVariables()
                .Build();

            var conexion = configuracion
                .GetConnectionString("ImportNet")
                ?? throw new InvalidOperationException(
                    "Configure la conexión local antes de probar.");

            var settings =
                new NpgsqlConnectionStringBuilder(conexion);

            if (settings.Database != "ImportNet"
                || settings.Username != "postgres"
                || settings.Host is not
                    ("localhost" or "127.0.0.1" or "::1"))
            {
                throw new InvalidOperationException(
                    "Esta prueba requiere ImportNet en PostgreSQL local con usuario postgres.");
            }

            _administrador = new NpgsqlConnection(
                settings.ConnectionString);

            try
            {
                _administrador.Open();

                using (var crear = new NpgsqlCommand(
                    $"CREATE SCHEMA \"{_esquema}\"",
                    _administrador))
                {
                    crear.ExecuteNonQuery();
                }

                _esquemaCreado = true;

                // Todas las tablas de esta prueba se resuelven
                // exclusivamente dentro del esquema temporal.
                settings.SearchPath = _esquema;

                var configPruebas =
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["ConnectionStrings:ImportNet"] =
                                    settings.ConnectionString
                            })
                        .Build();

                var services = new ServiceCollection();

                services.AddLogging();
                services.AddSingleton<IConfiguration>(
                    configPruebas);

                services.AddApplication()
                    .AddInfrastructure(configPruebas);

                services.AddSingleton<IUsuarioActual>(
                    new ActorPrueba());

                services.RemoveAll<AppDbContext>();

                services.AddScoped(_ =>
                    new AppDbContext(
                        new DbContextOptionsBuilder<AppDbContext>()
                            .UseNpgsql(settings.ConnectionString)
                            .AddInterceptors(_barrera)
                            .Options));

                _provider = services.BuildServiceProvider();

                using var scope = CrearScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                db.Database.ExecuteSqlRaw(
                    db.Database.GenerateCreateScript());
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public IServiceScope CrearScope()
        {
            return (_provider
                ?? throw new InvalidOperationException(
                    "El escenario no está preparado."))
                .CreateScope();
        }

        public async Task<(int Primera, int Segunda)> Preparar(
            bool comprasDiferentes)
        {
            using var scope = CrearScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var categoria = new Categoria("Menaje");

            var proveedor = new Proveedor
            {
                Nombre = "Proveedor temporal",
                Pais = "China",
                Contacto = "Pruebas"
            };

            var usuario = new ApplicationUser
            {
                Id = "usuario-concurrencia",
                UserName = "concurrencia@example.com",
                Email = "concurrencia@example.com"
            };

            db.AddRange(categoria, proveedor, usuario);
            await db.SaveChangesAsync();

            var producto = new Producto(
                "OLLA-CONCURRENCIA",
                "Olla temporal",
                null,
                "Unidad",
                null,
                100m,
                null,
                categoria.Id,
                2m);

            producto.ActualizarCostoPromedioPorRecepcion(
                0m,
                7m,
                140m);

            var empleado = new Empleado
            {
                UsuarioId = usuario.Id,
                Nombre = "Usuario",
                Apellidos = "Temporal",
                DocumentoIdentidad = "00000004",
                Cargo = "Administrador",
                Area = "Pruebas"
            };

            db.AddRange(producto, empleado);
            await db.SaveChangesAsync();

            var primera = new Compra(
                proveedor.Id,
                DateTime.UtcNow,
                3.75m,
                10m,
                5m,
                [new LineaCompra(producto.Id, 2m, 10m)]);

            Compra? segunda = null;

            if (comprasDiferentes)
            {
                segunda = new Compra(
                    proveedor.Id,
                    DateTime.UtcNow,
                    3m,
                    10m,
                    5m,
                    [new LineaCompra(producto.Id, 3m, 5m)]);

                db.Add(segunda);
            }

            db.AddRange(
                primera,
                new Inventario
                {
                    ProductoId = producto.Id,
                    AlmacenId = Almacen.PrincipalId,
                    Cantidad = 7m
                });

            await db.SaveChangesAsync();

            return (
                primera.Id,
                segunda?.Id ?? primera.Id);
        }

        public async Task<Resultado> Recibir(int compraId)
        {
            using var scope = CrearScope();

            var mediator = scope.ServiceProvider
                .GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(
                    new RecibirCompraCommand(compraId));

                return new Resultado(compraId, true);
            }
            catch (ConflictoException)
            {
                return new Resultado(compraId, false);
            }
        }

        public void Dispose()
        {
            try
            {
                _provider?.Dispose();
            }
            finally
            {
                try
                {
                    if (_esquemaCreado)
                    {
                        using var eliminar = new NpgsqlCommand(
                            $"DROP SCHEMA \"{_esquema}\" CASCADE",
                            _administrador);

                        eliminar.ExecuteNonQuery();
                        _esquemaCreado = false;
                    }
                }
                finally
                {
                    _administrador.Dispose();
                }
            }
        }
    }
}