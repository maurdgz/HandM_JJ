namespace ImportNet.Application.UseCases.Ventas;

public interface IAnulacionVentas
{
    Task<VentaAnuladaDto> Anular(int ventaId, string motivo, int empleadoId, CancellationToken ct);
}
