using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventarios;

public sealed record RegistrarAjusteCommand(int ProductoId, string Motivo, decimal Cantidad, string? Observacion)
    : IRequest<AjusteRegistradoDto>;

public sealed record AjusteRegistradoDto(
    int MovimientoId,
    int ProductoId,
    string Motivo,
    decimal Diferencia,
    decimal StockAnterior,
    decimal StockNuevo,
    DateTime Fecha);

internal sealed class RegistrarAjusteCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<RegistrarAjusteCommand, AjusteRegistradoDto>
{
    public async Task<AjusteRegistradoDto> Handle(RegistrarAjusteCommand request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        var motivo = AjusteInventario.Interpretar(request.Motivo);

        return await unitOfWork.EjecutarEnTransaccionAsync(async ct =>
        {
            var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, ct);

            if (await unitOfWork.Productos.ObtenerPorIdAsync(request.ProductoId, ct) == null)
                throw new NoEncontradoException("El producto no existe.");

            var inventario = await unitOfWork.Inventarios.ObtenerAsync(request.ProductoId, Almacen.PrincipalId, ct);
            var stockAnterior = inventario == null ? 0m : inventario.Cantidad;

            var movimiento = AjusteInventario.Crear(
                request.ProductoId,
                motivo,
                request.Cantidad,
                request.Observacion,
                stockAnterior,
                empleado.Id,
                DateTime.UtcNow);

            if (inventario == null)
            {
                inventario = new Inventario
                {
                    ProductoId = request.ProductoId,
                    AlmacenId = Almacen.PrincipalId,
                    Cantidad = 0m
                };
                unitOfWork.Inventarios.Agregar(inventario);
            }

            inventario.Cantidad = stockAnterior + movimiento.Cantidad;
            unitOfWork.Movimientos.Agregar(movimiento);
            await unitOfWork.GuardarAsync(ct);

            return new AjusteRegistradoDto(
                movimiento.Id,
                movimiento.ProductoId,
                motivo.ToString(),
                movimiento.Cantidad,
                stockAnterior,
                inventario.Cantidad,
                movimiento.Fecha);
        }, cancellationToken);
    }
}
