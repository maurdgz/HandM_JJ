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
        Terceros = new TercerosRepository(context);
        Categorias = new CategoriaRepository(context);
        Productos = new ProductoRepository(context);
        Inventarios = new InventarioRepository(context);
        Movimientos = new MovimientoRepository(context);
    }

    public ICompraRepository Compras { get; }
    public ITercerosRepository Terceros { get; }
    public IClienteRepository Clientes => Terceros;
    public IProveedorRepository Proveedores => Terceros;
    public ICategoriaRepository Categorias { get; }
    public IProductoRepository Productos { get; }
    public IInventarioRepository Inventarios { get; }
    public IMovimientoRepository Movimientos { get; }

    public async Task<int> Guardar(CancellationToken ct)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException(
                "El registro cambió; recargue e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: "23505" or "23503"
            })
        {
            throw new ConflictoException(
                "El registro está duplicado o tiene referencias; recargue los datos.");
        }
    }

    public async Task<T> EjecutarEnTransaccion<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (_context.Database.CurrentTransaction is not null)
            throw new ConflictoException(
                "Ya existe una transacción activa en esta unidad de trabajo.");

        await using var transaccion =
            await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var resultado = await operacion(ct);

            await Guardar(ct);
            await transaccion.CommitAsync(ct);

            return resultado;
        }
        catch (Exception falloOriginal)
        {
            try
            {
                await transaccion.RollbackAsync(
                    CancellationToken.None);
            }
            catch (Exception falloRollback)
            {
                throw new AggregateException(
                    "Falló la operación y también su reversión.",
                    falloOriginal,
                    falloRollback);
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }

            throw;
        }
    }
}