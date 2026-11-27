using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record AnularVentaCommand(int VentaId, string Motivo) : IRequest<VentaAnuladaDto>;

public sealed record StockRepuestoDto(int ProductoId, decimal Cantidad, decimal StockNuevo);

public sealed record VentaAnuladaDto(
    int Id,
    int Numero,
    string Estado,
    string Motivo,
    DateTime FechaAnulacion,
    IReadOnlyList<StockRepuestoDto> Reposiciones);

internal sealed class AnularVentaCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<AnularVentaCommand, VentaAnuladaDto>
{
    public async Task<VentaAnuladaDto> Handle(AnularVentaCommand request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new ReglaNegocioException("Indique el motivo de la anulación.");

        return await unitOfWork.EjecutarEnTransaccionAsync(async ct =>
        {
            var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, ct);

            var venta = await unitOfWork.Ventas.ObtenerConDetallesAsync(request.VentaId, ct)
                ?? throw new NoEncontradoException("La venta no existe.");

            if (venta.Estado == EstadoVenta.Anulada)
                throw new ConflictoException("La venta ya está anulada; el stock no se repone otra vez.");

            venta.Anular(request.Motivo);
            var fecha = venta.FechaAnulacion!.Value;
            var reposiciones = new List<StockRepuestoDto>();

            var productoIds = venta.Detalles.Select(d => d.ProductoId).Distinct().ToList();
            var inventarios = await unitOfWork.Inventarios.ObtenerPorProductosAsync(productoIds, Almacen.PrincipalId, ct);

            foreach (var linea in venta.Detalles)
            {
                if (!inventarios.TryGetValue(linea.ProductoId, out var inventario))
                {
                    inventario = new Inventario
                    {
                        ProductoId = linea.ProductoId,
                        AlmacenId = Almacen.PrincipalId,
                        Cantidad = 0m
                    };
                    unitOfWork.Inventarios.Agregar(inventario);
                }

                inventario.Cantidad += linea.Cantidad;

                unitOfWork.Movimientos.Agregar(new MovimientoInventario
                {
                    ProductoId = linea.ProductoId,
                    AlmacenId = Almacen.PrincipalId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = linea.Cantidad,
                    Fecha = fecha,
                    EmpleadoId = empleado.Id,
                    VentaId = venta.Id,
                    Observacion = venta.MotivoAnulacion
                });

                reposiciones.Add(new StockRepuestoDto(linea.ProductoId, linea.Cantidad, inventario.Cantidad));
            }

            return new VentaAnuladaDto(
                venta.Id,
                venta.Numero,
                venta.Estado.ToString(),
                venta.MotivoAnulacion!,
                fecha,
                reposiciones);
        }, cancellationToken);
    }
}
