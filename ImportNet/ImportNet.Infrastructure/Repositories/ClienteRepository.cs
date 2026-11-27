using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ClienteRepository(AppDbContext context)
    : GenericRepository<Cliente>(context), IClienteRepository
{
    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? texto, CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var busqueda = texto.ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(busqueda) || c.NumeroDocumento.Contains(texto));
        }

        return await query
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .Take(100)
            .ToListAsync(ct);
    }

    public Task<Cliente?> BuscarPorDocumentoAsync(string documento, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.NumeroDocumento == documento, ct);

    public Task<bool> DocumentoExisteAsync(string documento, int excluirId, CancellationToken ct = default)
        => _dbSet.AnyAsync(c => c.NumeroDocumento == documento && c.Id != excluirId, ct);

    public async Task EliminarClienteAsync(int id, CancellationToken ct = default)
    {
        var cliente = await ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");

        var tieneVentas = await _context.Set<Venta>().AnyAsync(v => v.ClienteId == id, ct);
        if (cliente.NumeroDocumento == "00000000" || tieneVentas)
            throw new ConflictoException("Cliente reservado o con ventas.");

        Eliminar(cliente);
    }
}
