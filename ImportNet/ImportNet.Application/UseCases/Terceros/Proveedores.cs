using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Terceros;
public sealed record ListarProveedoresQuery(string? Texto = null) : IRequest<IReadOnlyList<Proveedor>>;
internal sealed class ListarProveedoresQueryHandler(IUnitOfWork uow) : IRequestHandler<ListarProveedoresQuery, IReadOnlyList<Proveedor>>
{ public Task<IReadOnlyList<Proveedor>> Handle(ListarProveedoresQuery r, CancellationToken ct) => uow.Proveedores.Proveedores(r.Texto, ct); }
public sealed record ProveedorDetalle(Proveedor Proveedor, IReadOnlyList<int> Productos);
public sealed record ObtenerProveedorQuery(int Id) : IRequest<ProveedorDetalle>;
internal sealed class ObtenerProveedorQueryHandler(IUnitOfWork uow) : IRequestHandler<ObtenerProveedorQuery, ProveedorDetalle>
{ public async Task<ProveedorDetalle> Handle(ObtenerProveedorQuery r, CancellationToken ct) => new(await uow.Proveedores.Proveedor(r.Id, ct) ?? throw new NoEncontradoException("Proveedor no encontrado."), await uow.Proveedores.Productos(r.Id, ct)); }
public sealed record GuardarProveedorCommand(int Id, string Nombre, TipoProveedor Tipo, string Pais, string Contacto, bool Activo, int[] Productos) : IRequest<int>;
internal sealed class GuardarProveedorCommandHandler(IUnitOfWork uow) : IRequestHandler<GuardarProveedorCommand, int>
{
    public async Task<int> Handle(GuardarProveedorCommand r, CancellationToken ct)
    {
        if (!Enum.IsDefined(r.Tipo) || r.Productos is null || r.Productos.Distinct().Count() != r.Productos.Length) throw new ReglaNegocioException("Tipo o productos invalidos.");
        var nombre = Reglas.Texto(r.Nombre, 200, "Nombre"); var pais = Reglas.Texto(r.Pais, 100, "Pais"); var contacto = Reglas.Texto(r.Contacto, 200, "Contacto");
        var p = r.Id == 0 ? new Proveedor() : await uow.Proveedores.Proveedor(r.Id, ct) ?? throw new NoEncontradoException("Proveedor no encontrado.");
        p.Nombre = nombre; p.Pais = pais; p.Tipo = r.Tipo; p.Contacto = contacto; p.Activo = r.Activo;
        await uow.Proveedores.GuardarProveedor(p, r.Productos, ct); await uow.Guardar(ct); return p.Id;
    }
}
public sealed record EliminarProveedorCommand(int Id) : IRequest;
internal sealed class EliminarProveedorCommandHandler(IUnitOfWork uow) : IRequestHandler<EliminarProveedorCommand>
{ public async Task Handle(EliminarProveedorCommand r, CancellationToken ct) { await uow.Proveedores.EliminarProveedor(r.Id, ct); await uow.Guardar(ct); } }
