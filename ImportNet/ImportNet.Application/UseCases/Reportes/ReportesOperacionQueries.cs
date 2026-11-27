using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record StockBajoMinimoQuery : IRequest<IReadOnlyList<ProductoStockBajo>>;

/// <summary>Cierre de caja de todos los vendedores. Solo Administrador.</summary>
public sealed record CierreCajaQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

/// <summary>Cierre de caja limitado a las ventas del usuario actual.</summary>
public sealed record MiResumenDiarioQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

public sealed record ComprasPorProveedorQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null)
    : IRequest<IReadOnlyList<ComprasPorProveedor>>;

internal sealed class StockBajoMinimoQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<StockBajoMinimoQuery, IReadOnlyList<ProductoStockBajo>>
{
    public async Task<IReadOnlyList<ProductoStockBajo>> Handle(StockBajoMinimoQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        return await uow.Reportes.StockBajoMinimo(ct);
    }
}

internal sealed class CierreCajaQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<CierreCajaQuery, CierreCaja>
{
    public async Task<CierreCaja> Handle(CierreCajaQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var dia = request.Fecha ?? PeriodoReporte.Hoy();
        var periodo = PeriodoReporte.Dia(dia);
        return await uow.Reportes.CierreCaja(dia, periodo.DesdeUtc, periodo.HastaUtc, null, ct);
    }
}

internal sealed class MiResumenDiarioQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<MiResumenDiarioQuery, CierreCaja>
{
    public async Task<CierreCaja> Handle(MiResumenDiarioQuery request, CancellationToken ct)
    {
        var usuarioId = actor.Id ?? throw new AccesoNoPermitidoException(401);
        var empleado = await empleados.ObtenerPorUsuario(usuarioId, ct);

        if (empleado is not { Activo: true })
            throw new AccesoNoPermitidoException(403);

        var dia = request.Fecha ?? PeriodoReporte.Hoy();
        var periodo = PeriodoReporte.Dia(dia);

        // El empleado sale del token: nadie puede pedir el resumen de otro.
        return await uow.Reportes.CierreCaja(dia, periodo.DesdeUtc, periodo.HastaUtc, empleado.Id, ct);
    }
}

internal sealed class ComprasPorProveedorQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<ComprasPorProveedorQuery, IReadOnlyList<ComprasPorProveedor>>
{
    public async Task<IReadOnlyList<ComprasPorProveedor>> Handle(ComprasPorProveedorQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await uow.Reportes.ComprasPorProveedor(periodo.DesdeUtc, periodo.HastaUtc, ct);
    }
}
