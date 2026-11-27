using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class ReporteRepository(AppDbContext context) : IReporteRepository
{
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    private IQueryable<Venta> VentasRegistradas(DateTime desdeUtc, DateTime hastaUtc)
        => context.Set<Venta>()
            .AsNoTracking()
            .Where(v => v.Estado == EstadoVenta.Registrada && v.Fecha >= desdeUtc && v.Fecha < hastaUtc);

    private IQueryable<DetalleVenta> DetallesVendidos(DateTime desdeUtc, DateTime hastaUtc)
        => context.Set<DetalleVenta>()
            .AsNoTracking()
            .Where(d => d.Venta.Estado == EstadoVenta.Registrada && d.Venta.Fecha >= desdeUtc && d.Venta.Fecha < hastaUtc);

    private static decimal Redondear(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    public async Task<List<VentasPorPeriodo>> VentasPorPeriodoAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        AgrupacionPeriodo agrupacion,
        CancellationToken ct = default)
    {
        var ventas = await VentasRegistradas(desdeUtc, hastaUtc)
            .Select(v => new { v.Fecha, v.Total })
            .ToListAsync(ct);

        var formato = agrupacion == AgrupacionPeriodo.Mes ? "yyyy-MM" : "yyyy-MM-dd";

        return ventas
            .GroupBy(v => (v.Fecha + HoraPeru).ToString(formato))
            .Select(g => new VentasPorPeriodo(g.Key, g.Count(), g.Sum(v => v.Total)))
            .OrderBy(x => x.Periodo, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<List<VentasPorVendedor>> VentasPorVendedorAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct = default)
    {
        var grupos = await VentasRegistradas(desdeUtc, hastaUtc)
            .GroupBy(v => new { v.EmpleadoId, v.Empleado.Nombre, v.Empleado.Apellidos })
            .Select(g => new
            {
                g.Key.EmpleadoId,
                g.Key.Nombre,
                g.Key.Apellidos,
                Ventas = g.Count(),
                Total = g.Sum(v => v.Total)
            })
            .ToListAsync(ct);

        return grupos
            .Select(g => new VentasPorVendedor(
                g.EmpleadoId,
                (g.Nombre + " " + g.Apellidos).Trim(),
                g.Ventas,
                g.Total,
                Redondear(g.Total / g.Ventas)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<List<TopProducto>> TopProductosAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int top,
        CancellationToken ct = default)
    {
        var grupos = await DetallesVendidos(desdeUtc, hastaUtc)
            .GroupBy(d => new { d.ProductoId, d.Producto.Codigo, d.Producto.Nombre, d.Producto.UnidadMedida })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.UnidadMedida,
                Cantidad = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Subtotal)
            })
            .ToListAsync(ct);

        return grupos
            .Select(g => new TopProducto(g.ProductoId, g.Codigo, g.Nombre, g.UnidadMedida, g.Cantidad, g.Total))
            .OrderByDescending(x => x.Cantidad)
            .ThenByDescending(x => x.Total)
            .ThenBy(x => x.ProductoId)
            .Take(top)
            .ToList();
    }

    public async Task<List<MargenProducto>> MargenPorProductoAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct = default)
    {
        var grupos = await DetallesVendidos(desdeUtc, hastaUtc)
            .GroupBy(d => new { d.ProductoId, d.Producto.Codigo, d.Producto.Nombre, d.Producto.CostoPromedio })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.CostoPromedio,
                Cantidad = g.Sum(d => d.Cantidad),
                Ingreso = g.Sum(d => d.Subtotal)
            })
            .ToListAsync(ct);

        var resultado = new List<MargenProducto>();

        foreach (var g in grupos)
        {
            var costo = Redondear(g.Cantidad * g.CostoPromedio);
            var margen = g.Ingreso - costo;
            var porcentaje = g.Ingreso == 0 ? 0m : Redondear(margen / g.Ingreso * 100);

            resultado.Add(new MargenProducto(
                g.ProductoId,
                g.Codigo,
                g.Nombre,
                g.Cantidad,
                g.Ingreso,
                g.CostoPromedio,
                costo,
                margen,
                porcentaje));
        }

        return resultado
            .OrderByDescending(x => x.Margen)
            .ThenBy(x => x.ProductoId)
            .ToList();
    }

    public async Task<List<ProductoStockBajo>> StockBajoMinimoAsync(CancellationToken ct = default)
    {
        var productos = await context.Set<Producto>()
            .AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new
            {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.UnidadMedida,
                p.StockMinimo,
                Stock = p.Inventarios.Where(i => i.AlmacenId == Almacen.PrincipalId).Sum(i => i.Cantidad)
            })
            .Where(p => p.Stock <= p.StockMinimo)
            .ToListAsync(ct);

        return productos
            .Select(p => new ProductoStockBajo(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.UnidadMedida,
                p.Stock,
                p.StockMinimo,
                p.StockMinimo - p.Stock))
            .OrderByDescending(x => x.Faltante)
            .ThenBy(x => x.Nombre, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<CierreCaja> CierreCajaAsync(
        DateOnly fecha,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? empleadoId,
        CancellationToken ct = default)
    {
        var query = context.Set<Venta>()
            .AsNoTracking()
            .Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc);

        if (empleadoId != null)
            query = query.Where(v => v.EmpleadoId == empleadoId);

        var ventas = await query
            .Select(v => new
            {
                v.EmpleadoId,
                EmpleadoNombre = v.Empleado.Nombre,
                EmpleadoApellidos = v.Empleado.Apellidos,
                v.Estado,
                v.Subtotal,
                v.Descuento,
                v.Total
            })
            .ToListAsync(ct);

        var registradas = ventas.Where(v => v.Estado == EstadoVenta.Registrada).ToList();
        var anuladas = ventas.Where(v => v.Estado == EstadoVenta.Anulada).ToList();

        var porVendedor = registradas
            .GroupBy(v => new { v.EmpleadoId, Vendedor = (v.EmpleadoNombre + " " + v.EmpleadoApellidos).Trim() })
            .Select(g => new CierreVendedor(g.Key.EmpleadoId, g.Key.Vendedor, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.Ordinal)
            .ToList();

        return new CierreCaja(
            fecha,
            registradas.Count,
            registradas.Sum(v => v.Subtotal),
            registradas.Sum(v => v.Descuento),
            registradas.Sum(v => v.Total),
            anuladas.Count,
            anuladas.Sum(v => v.Total),
            porVendedor);
    }

    public async Task<List<ComprasPorProveedor>> ComprasPorProveedorAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct = default)
    {
        var compras = await context.Set<Compra>()
            .AsNoTracking()
            .Where(c => c.Estado == EstadoCompra.Recibida && c.Fecha >= desdeUtc && c.Fecha < hastaUtc)
            .Select(c => new
            {
                c.ProveedorId,
                ProveedorNombre = c.Proveedor.Nombre,
                ProveedorPais = c.Proveedor.Pais,
                SubtotalUsd = c.Lineas.Sum(l => l.Cantidad * l.CostoUnitarioUsd),
                TotalPen = c.Lineas.Sum(l => l.MercaderiaPen + l.FletePen + l.AduanaPen)
            })
            .ToListAsync(ct);

        return compras
            .GroupBy(c => new { c.ProveedorId, c.ProveedorNombre, c.ProveedorPais })
            .Select(g => new ComprasPorProveedor(
                g.Key.ProveedorId,
                g.Key.ProveedorNombre,
                g.Key.ProveedorPais,
                g.Count(),
                Redondear(g.Sum(c => c.SubtotalUsd)),
                g.Sum(c => c.TotalPen)))
            .OrderByDescending(x => x.TotalPen)
            .ThenBy(x => x.Proveedor, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<ResumenDashboard> DashboardAsync(
        DateOnly fecha,
        DateTime inicioDiaUtc,
        DateTime finDiaUtc,
        DateTime inicioMesUtc,
        CancellationToken ct = default)
    {
        var ventasMes = await VentasRegistradas(inicioMesUtc, finDiaUtc)
            .Select(v => new { v.Fecha, v.Total })
            .ToListAsync(ct);

        var ventasHoy = ventasMes.Where(v => v.Fecha >= inicioDiaUtc).ToList();
        var totalMes = ventasMes.Sum(v => v.Total);
        var ticketPromedio = ventasMes.Count > 0 ? Redondear(totalMes / ventasMes.Count) : 0m;

        var lineasMes = await DetallesVendidos(inicioMesUtc, finDiaUtc)
            .Select(d => new { d.ProductoId, d.Producto.CostoPromedio, d.Cantidad, d.Subtotal })
            .ToListAsync(ct);

        var margenMes = lineasMes
            .GroupBy(l => new { l.ProductoId, l.CostoPromedio })
            .Sum(g => g.Sum(l => l.Subtotal) - Redondear(g.Sum(l => l.Cantidad) * g.Key.CostoPromedio));

        var productos = await context.Set<Producto>()
            .AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new
            {
                p.StockMinimo,
                Cantidad = p.Inventarios.Where(i => i.AlmacenId == Almacen.PrincipalId).Sum(i => i.Cantidad)
            })
            .ToListAsync(ct);

        return new ResumenDashboard(
            fecha,
            ventasHoy.Sum(v => v.Total),
            ventasHoy.Count,
            totalMes,
            ventasMes.Count,
            ticketPromedio,
            margenMes,
            productos.Count,
            productos.Count(p => p.Cantidad <= p.StockMinimo),
            productos.Count(p => p.Cantidad == 0));
    }
}
