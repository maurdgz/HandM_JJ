using ImportNet.Application.Interfaces;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Reportes;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.AsistenteIa;

public sealed record AnalizarNegocioIaCommand(DateOnly? Desde = null, DateOnly? Hasta = null, string? Pregunta = null)
    : IRequest<AnalisisIaDto>;

internal sealed class AnalizarNegocioIaCommandHandler(
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    IAsistenteIaService asistenteIa) : IRequestHandler<AnalizarNegocioIaCommand, AnalisisIaDto>
{
    public async Task<AnalisisIaDto> Handle(AnalizarNegocioIaCommand request, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirAdministradorAsync(cancellationToken);

        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        var (desde, hasta) = PeriodoReporte.Fechas(request.Desde, request.Hasta);

        var hoy = FechasPeru.Hoy();
        var inicioDiaUtc = FechasPeru.InicioDelDiaUtc(hoy);
        var finDiaUtc = FechasPeru.InicioDelDiaUtc(hoy.AddDays(1));
        var inicioMesUtc = FechasPeru.InicioDelDiaUtc(new DateOnly(hoy.Year, hoy.Month, 1));

        var dashboard = await unitOfWork.Reportes.DashboardAsync(hoy, inicioDiaUtc, finDiaUtc, inicioMesUtc, cancellationToken);
        var topProductos = await unitOfWork.Reportes.TopProductosAsync(periodo.DesdeUtc, periodo.HastaUtc, 10, cancellationToken);
        var margenProductos = await unitOfWork.Reportes.MargenPorProductoAsync(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
        var stockBajo = await unitOfWork.Reportes.StockBajoMinimoAsync(cancellationToken);

        var contexto = new ContextoNegocioIa(desde, hasta, dashboard, topProductos, margenProductos, stockBajo);
        var pregunta = string.IsNullOrWhiteSpace(request.Pregunta) ? null : request.Pregunta.Trim();
        var respuesta = await asistenteIa.AnalizarNegocioAsync(contexto, pregunta, cancellationToken);

        return new AnalisisIaDto(
            desde,
            hasta,
            respuesta.ResumenEjecutivo,
            respuesta.Recomendaciones,
            respuesta.AlertasPrioritarias,
            respuesta.RespuestaPregunta,
            respuesta.ProveedorIa,
            respuesta.Modelo,
            DateTime.UtcNow);
    }
}
