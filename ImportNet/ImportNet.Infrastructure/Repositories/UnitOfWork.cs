using System.Data;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Compras = new CompraRepository(context);
        Clientes = new ClienteRepository(context);
        Proveedores = new ProveedorRepository(context);
        Categorias = new CategoriaRepository(context);
        Productos = new ProductoRepository(context);
        Inventarios = new InventarioRepository(context);
        Movimientos = new MovimientoRepository(context);
        Reportes = new ReporteRepository(context);
        Ventas = new VentaRepository(context);
    }

    public ICompraRepository Compras { get; }
    public IClienteRepository Clientes { get; }
    public IProveedorRepository Proveedores { get; }
    public ICategoriaRepository Categorias { get; }
    public IProductoRepository Productos { get; }
    public IInventarioRepository Inventarios { get; }
    public IMovimientoRepository Movimientos { get; }
    public IReporteRepository Reportes { get; }
    public IVentaRepository Ventas { get; }

    public async Task<int> Guardar(CancellationToken ct)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException("El registro cambió; recargue e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        {
            throw new ConflictoException("El registro está duplicado o tiene referencias; recargue los datos.");
        }
    }

    public async Task<T> EjecutarEnTransaccion<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var resultado = await operacion(ct);
            await Guardar(ct);
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch (Exception ex) when (EsConflictoDeSerializacion(ex))
        {
            _context.ChangeTracker.Clear();
            throw new ConflictoException("Otra operación modificó los mismos datos. Recargue y vuelva a intentarlo.");
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private static bool EsConflictoDeSerializacion(Exception ex)
    {
        var postgres = ex as PostgresException ?? ex.InnerException as PostgresException;
        return postgres != null && (postgres.SqlState == "40001" || postgres.SqlState == "40P01");
    }
}
