using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Cliente>> Clientes(string? texto, CancellationToken ct)
    {
        var query = _context.Set<Cliente>().AsNoTracking();

        if (texto != null)
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

    public Task<Cliente?> Cliente(int id, CancellationToken ct)
    {
        return _context.Set<Cliente>().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<Cliente?> BuscarPorDocumento(string documento, CancellationToken ct)
    {
        return _context.Set<Cliente>().AsNoTracking().FirstOrDefaultAsync(c => c.NumeroDocumento == documento, ct);
    }

    public Task<bool> DocumentoExiste(string documento, int excluirId, CancellationToken ct)
    {
        return _context.Set<Cliente>().AnyAsync(c => c.NumeroDocumento == documento && c.Id != excluirId, ct);
    }

    public void AgregarCliente(Cliente cliente)
    {
        _context.Set<Cliente>().Add(cliente);
    }

    public async Task EliminarCliente(int id, CancellationToken ct)
    {
        var cliente = await Cliente(id, ct);
        if (cliente == null)
            throw new NoEncontradoException("Cliente no encontrado.");

        var tieneVentas = await _context.Set<Venta>().AnyAsync(v => v.ClienteId == id, ct);
        if (cliente.NumeroDocumento == "00000000" || tieneVentas)
            throw new ConflictoException("Cliente reservado o con ventas.");

        _context.Set<Cliente>().Remove(cliente);
    }
}
