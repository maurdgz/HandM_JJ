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
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            var resultado = await operacion(ct);

            await Guardar(ct);
            await transaccion.CommitAsync(ct);

            return resultado;
        }
        catch (Exception error)
        {
            _context.ChangeTracker.Clear();

            if (EsConflictoDeConcurrencia(error))
            {
                throw new ConflictoException(
                    "Otra operación modificó los mismos datos. Recargue y vuelva a intentarlo.");
            }

            throw;
        }
    }

    private static bool EsConflictoDeConcurrencia(
        Exception error)
    {
        if (error is DbUpdateConcurrencyException)
            return true;

        if (error is PostgresException postgres
            && postgres.SqlState is
                "40001" or "40P01" or "23505" or "23503")
            return true;

        return error.InnerException is { } interna
            && EsConflictoDeConcurrencia(interna);
    }
}