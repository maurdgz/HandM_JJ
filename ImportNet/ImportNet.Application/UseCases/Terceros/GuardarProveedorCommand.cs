using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record GuardarProveedorCommand(int Id, string Nombre, TipoProveedor Tipo, string Pais, string Contacto, bool Activo,
    int[] Productos) : IRequest<int>;

internal sealed class GuardarProveedorCommandHandler : IRequestHandler<GuardarProveedorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GuardarProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GuardarProveedorCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Tipo) || request.Productos == null || request.Productos.Distinct().Count() != request.Productos.Length)
            throw new ReglaNegocioException("Tipo o productos invalidos.");

        var nombre = Reglas.Texto(request.Nombre, 200, "Nombre");
        var pais = Reglas.Texto(request.Pais, 100, "Pais");
        var contacto = Reglas.Texto(request.Contacto, 200, "Contacto");

        Proveedor proveedor;
        if (request.Id == 0)
        {
            proveedor = new Proveedor();
        }
        else
        {
            proveedor = await _unitOfWork.Proveedores.Proveedor(request.Id, cancellationToken)
                ?? throw new NoEncontradoException("Proveedor no encontrado.");
        }

        proveedor.Nombre = nombre;
        proveedor.Pais = pais;
        proveedor.Tipo = request.Tipo;
        proveedor.Contacto = contacto;
        proveedor.Activo = request.Activo;

        await _unitOfWork.Proveedores.GuardarProveedor(proveedor, request.Productos, cancellationToken);
        await _unitOfWork.Guardar(cancellationToken);
        return proveedor.Id;
    }
}
