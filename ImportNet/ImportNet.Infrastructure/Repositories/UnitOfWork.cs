using System.Data;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;
    private ICompraRepository? _compras;
    private IClienteRepository? _clientes;
    private IProveedorRepository? _proveedores;
    private ICategoriaRepository? _categorias;
    private IProductoRepository? _productos;
    private IInventarioRepository? _inventarios;
    private IMovimientoRepository? _movimientos;
    private IReporteRepository? _reportes;
    private IVentaRepository? _ventas;
    private IEmpleadoRepository? _empleados;

    public ICompraRepository Compras => _compras ??= new CompraRepository(_context);
    public IClienteRepository Clientes => _clientes ??= new ClienteRepository(_context);
    public IProveedorRepository Proveedores => _proveedores ??= new ProveedorRepository(_context);
    public ICategoriaRepository Categorias => _categorias ??= new CategoriaRepository(_context);
    public IProductoRepository Productos => _productos ??= new ProductoRepository(_context);
    public IInventarioRepository Inventarios => _inventarios ??= new InventarioRepository(_context);
    public IMovimientoRepository Movimientos => _movimientos ??= new MovimientoRepository(_context);
    public IReporteRepository Reportes => _reportes ??= new ReporteRepository(_context);
    public IVentaRepository Ventas => _ventas ??= new VentaRepository(_context);
    public IEmpleadoRepository Empleados => _empleados ??= new EmpleadoRepository(_context);

    public async Task<int> GuardarAsync(CancellationToken ct = default)
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

    public async Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (_context.Database.CurrentTransaction is not null)
            throw new ConflictoException("Ya existe una transacción activa en esta unidad de trabajo.");

        await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var resultado = await operacion(ct);
            await GuardarAsync(ct);
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch (Exception ex)
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Ignorar fallo secundario al revertir transacción abortada
            }

            _context.ChangeTracker.Clear();

            if (EsConflictoDeConcurrencia(ex))
                throw new ConflictoException("Otra operación modificó los mismos datos. Recargue y vuelva a intentarlo.");

            throw;
        }
    }

    public void Dispose() => _context.Dispose();

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private static bool EsConflictoDeConcurrencia(Exception error)
    {
        if (error is DbUpdateConcurrencyException)
            return true;

        if (error is PostgresException { SqlState: "40001" or "40P01" or "23505" or "23503" })
            return true;

        return error.InnerException is { } interna && EsConflictoDeConcurrencia(interna);
    }
}
