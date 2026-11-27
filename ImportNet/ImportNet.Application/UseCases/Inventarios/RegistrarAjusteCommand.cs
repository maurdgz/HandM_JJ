using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventarios;

public sealed record RegistrarAjusteCommand(int ProductoId, string Motivo, decimal Cantidad, string? Observacion)
    : IRequest<AjusteRegistradoDto>;

public sealed record AjusteRegistradoDto(int MovimientoId, int ProductoId, string Motivo, decimal Diferencia,
    decimal StockAnterior, decimal StockNuevo, DateTime Fecha);

internal sealed class RegistrarAjusteCommandHandler : IRequestHandler<RegistrarAjusteCommand, AjusteRegistradoDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public RegistrarAjusteCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<AjusteRegistradoDto> Handle(RegistrarAjusteCommand request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        var motivo = AjusteInventario.Interpretar(request.Motivo);

        return await _unitOfWork.EjecutarEnTransaccion(async ct =>
        {
            var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, ct);

            if (await _unitOfWork.Productos.Obtener(request.ProductoId, ct) == null)
                throw new NoEncontradoException("El producto no existe.");

            var inventario = await _unitOfWork.Inventarios.Obtener(request.ProductoId, Almacen.PrincipalId, ct);
            var stockAnterior = inventario == null ? 0m : inventario.Cantidad;

            var movimiento = AjusteInventario.Crear(request.ProductoId, motivo, request.Cantidad, request.Observacion,
                stockAnterior, empleado.Id, DateTime.UtcNow);

            if (inventario == null)
            {
                inventario = new Inventario { ProductoId = request.ProductoId, AlmacenId = Almacen.PrincipalId, Cantidad = 0m };
                _unitOfWork.Inventarios.Agregar(inventario);
            }

            inventario.Cantidad = stockAnterior + movimiento.Cantidad;
            _unitOfWork.Movimientos.Agregar(movimiento);
            await _unitOfWork.Guardar(ct);

            return new AjusteRegistradoDto(movimiento.Id, movimiento.ProductoId, motivo.ToString(), movimiento.Cantidad,
                stockAnterior, inventario.Cantidad, movimiento.Fecha);
        }, cancellationToken);
    }
}
