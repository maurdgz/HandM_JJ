using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Terceros;
public sealed record ListarClientesQuery(string? Texto = null) : IRequest<IReadOnlyList<Cliente>>;
internal sealed class ListarClientesQueryHandler(IUnitOfWork uow) : IRequestHandler<ListarClientesQuery, IReadOnlyList<Cliente>>
{ public Task<IReadOnlyList<Cliente>> Handle(ListarClientesQuery r, CancellationToken ct) => uow.Terceros.Clientes(r.Texto, ct); }
public sealed record GuardarClienteCommand(int Id, TipoDocumento TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono) : IRequest<int>;
internal sealed class GuardarClienteCommandHandler(IUnitOfWork uow) : IRequestHandler<GuardarClienteCommand, int>
{
    public async Task<int> Handle(GuardarClienteCommand r, CancellationToken ct)
    {
        if (!Enum.IsDefined(r.TipoDocumento) || r.NumeroDocumento is null || r.NumeroDocumento.Length != (r.TipoDocumento == TipoDocumento.Dni ? 8 : 11) || r.NumeroDocumento.Any(c => c < '0' || c > '9') || r.NumeroDocumento == "00000000") throw new ReglaNegocioException("DNI de 8 digitos o RUC de 11 digitos requerido; documento reservado no editable.");
        var nombre = Reglas.Texto(r.Nombre, 200, "Nombre");
        if (r.Telefono?.Length > 20) throw new ReglaNegocioException("Telefono admite 20 caracteres.");
        if (await uow.Terceros.DocumentoExiste(r.NumeroDocumento, r.Id, ct)) throw new ConflictoException("Documento duplicado.");
        var c = r.Id == 0 ? new Cliente() : await uow.Terceros.Cliente(r.Id, ct) ?? throw new NoEncontradoException("Cliente no encontrado.");
        if (c.NumeroDocumento == "00000000") throw new ConflictoException("Cliente varios es reservado.");
        c.Nombre = nombre; c.TipoDocumento = r.TipoDocumento; c.NumeroDocumento = r.NumeroDocumento; c.Telefono = r.Telefono;
        if (r.Id == 0) uow.Terceros.AgregarCliente(c);
        await uow.Guardar(ct); return c.Id;
    }
}
public sealed record EliminarClienteCommand(int Id) : IRequest;
internal sealed class EliminarClienteCommandHandler(IUnitOfWork uow) : IRequestHandler<EliminarClienteCommand>
{ public async Task Handle(EliminarClienteCommand r, CancellationToken ct) { await uow.Terceros.EliminarCliente(r.Id, ct); await uow.Guardar(ct); } }
