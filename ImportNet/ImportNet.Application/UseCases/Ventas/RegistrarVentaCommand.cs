using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record LineaVentaEntrada(int ProductoId, decimal Cantidad, decimal Descuento);
public sealed record RegistrarVentaCommand(int? ClienteId, IReadOnlyList<LineaVentaEntrada> Lineas) : IRequest<VentaRegistradaDto>;
public sealed record VentaRegistradaDto(int Id, int Numero, DateTime Fecha, decimal Subtotal, decimal Descuento, decimal Total, IReadOnlyList<DetalleVentaDto> Lineas);
public sealed record DetalleVentaDto(int ProductoId, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal);

public interface IRegistroVenta
{
    Task<VentaRegistradaDto> Registrar(int empleadoId, int? clienteId, IReadOnlyList<LineaVentaEntrada> lineas, CancellationToken ct);
}

internal sealed class RegistrarVentaCommandHandler(IUsuarioActual usuario, IEmpleadoRepository empleados, IRegistroVenta registro)
    : IRequestHandler<RegistrarVentaCommand, VentaRegistradaDto>
{
    public async Task<VentaRegistradaDto> Handle(RegistrarVentaCommand request, CancellationToken ct)
    {
        if (usuario.Id is not { } userId) throw new AccesoNoPermitidoException(401);
        var empleado = await empleados.ObtenerPorUsuario(userId, ct);
        if (empleado is not { Activo: true }) throw new AccesoNoPermitidoException(403);
        return await registro.Registrar(empleado.Id, request.ClienteId, request.Lineas, ct);
    }
}
