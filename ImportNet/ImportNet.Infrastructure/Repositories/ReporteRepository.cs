using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

/// <remarks>
/// El filtro, los joins y la proyección se ejecutan en la base. SQLite (pruebas) no suma decimales
/// en el servidor, así que GroupBy/Sum/OrderByDescending/Take se aplican sobre las filas proyectadas,
/// igual que el saldo del kardex.
/// </remarks>
internal sealed class ReporteRepository(AppDbContext db) : IReporteRepository
{
    // Perú no usa horario de verano.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    private IQueryable<Venta> VentasRegistradas(DateTime desdeUtc, DateTime hastaUtc)
        => db.Set<Venta>()
            .AsNoTracking()
            .Where(x => x.Estado == EstadoVenta.Registrada
                && x.Fecha >= desdeUtc
                && x.Fecha < hastaUtc);

    public async Task<IReadOnlyList<VentasPorPeriodo>> VentasPorPeriodo(
        DateTime desdeUtc,
        DateTime hastaUtc,
        AgrupacionPeriodo agrupacion,
        CancellationToken ct)
    {
        var filas = await VentasRegistradas(desdeUtc, hastaUtc)
            .Select(x => new { x.Fecha, x.Total })
            .ToListAsync(ct);

        var formato = agrupacion == AgrupacionPeriodo.Mes ? "yyyy-MM" : "yyyy-MM-dd";

        return filas
            .GroupBy(x => (x.Fecha + HoraPeru).ToString(formato))
            .Select(g => new VentasPorPeriodo(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(x => x.Periodo, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<VentasPorVendedor>> VentasPorVendedor(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct)
    {
        var filas = await (
            from venta in VentasRegistradas(desdeUtc, hastaUtc)
            join empleado in db.Set<Empleado>().AsNoTracking()
                on venta.EmpleadoId equals empleado.Id
            select new
            {
                venta.EmpleadoId,
                Vendedor = empleado.Nombre + " " + empleado.Apellidos,
                venta.Total
            })
            .ToListAsync(ct);

        return filas
            .GroupBy(x => new { x.EmpleadoId, x.Vendedor })
            .Select(g =>
            {
                var total = g.Sum(x => x.Total);
                var ventas = g.Count();
                return new VentasPorVendedor(
                    g.Key.EmpleadoId,
                    g.Key.Vendedor.Trim(),
                    ventas,
                    total,
                    decimal.Round(total / ventas, 2, MidpointRounding.AwayFromZero));
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyList<LineaVendida>> LineasVendidas(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct)
        => await (
            from venta in VentasRegistradas(desdeUtc, hastaUtc)
            join detalle in db.Set<DetalleVenta>().AsNoTracking()
                on venta.Id equals detalle.VentaId
            join producto in db.Set<Producto>().AsNoTracking()
                on detalle.ProductoId equals producto.Id
            select new LineaVendida(
                producto.Id,
                producto.Codigo,
                producto.Nombre,
                producto.UnidadMedida,
                producto.CostoPromedio,
                detalle.Cantidad,
                detalle.Subtotal))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TopProducto>> TopProductos(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int top,
        CancellationToken ct)
    {
        var lineas = await LineasVendidas(desdeUtc, hastaUtc, ct);

        return lineas
            .GroupBy(x => new { x.ProductoId, x.Codigo, x.Nombre, x.UnidadMedida })
            .Select(g => new TopProducto(
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Nombre,
                g.Key.UnidadMedida,
                g.Sum(x => x.Cantidad),
                g.Sum(x => x.Subtotal)))
            .OrderByDescending(x => x.Cantidad)
            .ThenByDescending(x => x.Total)
            .ThenBy(x => x.ProductoId)
            .Take(top)
            .ToArray();
    }

    public async Task<IReadOnlyList<MargenProducto>> MargenPorProducto(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct)
    {
        var lineas = await LineasVendidas(desdeUtc, hastaUtc, ct);

        return lineas
            .GroupBy(x => new { x.ProductoId, x.Codigo, x.Nombre, x.CostoPromedio })
            .Select(g =>
            {
                var cantidad = g.Sum(x => x.Cantidad);
                var ingreso = g.Sum(x => x.Subtotal);
                var costo = decimal.Round(cantidad * g.Key.CostoPromedio, 2, MidpointRounding.AwayFromZero);
                var margen = ingreso - costo;
                var porcentaje = ingreso == 0
                    ? 0m
                    : decimal.Round(margen / ingreso * 100, 2, MidpointRounding.AwayFromZero);

                return new MargenProducto(
                    g.Key.ProductoId,
                    g.Key.Codigo,
                    g.Key.Nombre,
                    cantidad,
                    ingreso,
                    g.Key.CostoPromedio,
                    costo,
                    margen,
                    porcentaje);
            })
            .OrderByDescending(x => x.Margen)
            .ThenBy(x => x.ProductoId)
            .ToArray();
    }

    public async Task<IReadOnlyList<ProductoStockBajo>> StockBajoMinimo(CancellationToken ct)
    {
        var filas = await (
            from producto in db.Set<Producto>().AsNoTracking()
            join inventario in db.Set<Inventario>().AsNoTracking()
                    .Where(x => x.AlmacenId == Almacen.PrincipalId)
                on producto.Id equals inventario.ProductoId into existencias
            from inventario in existencias.DefaultIfEmpty()
            where producto.Activo
            select new
            {
                producto.Id,
                producto.Codigo,
                producto.Nombre,
                producto.UnidadMedida,
                producto.StockMinimo,
                Stock = inventario == null ? 0m : inventario.Cantidad
            })
            .Where(x => x.Stock <= x.StockMinimo)
            .ToListAsync(ct);

        return filas
            .Select(x => new ProductoStockBajo(
                x.Id,
                x.Codigo,
                x.Nombre,
                x.UnidadMedida,
                x.Stock,
                x.StockMinimo,
                x.StockMinimo - x.Stock))
            .OrderByDescending(x => x.Faltante)
            .ThenBy(x => x.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<CierreCaja> CierreCaja(
        DateOnly fecha,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? empleadoId,
        CancellationToken ct)
    {
        var ventas = db.Set<Venta>()
            .AsNoTracking()
            .Where(x => x.Fecha >= desdeUtc && x.Fecha < hastaUtc);

        if (empleadoId is { } empleado)
            ventas = ventas.Where(x => x.EmpleadoId == empleado);

        var filas = await (
            from venta in ventas
            join vendedor in db.Set<Empleado>().AsNoTracking()
                on venta.EmpleadoId equals vendedor.Id
            select new
            {
                venta.Estado,
                venta.EmpleadoId,
                Vendedor = vendedor.Nombre + " " + vendedor.Apellidos,
                venta.Subtotal,
                venta.Descuento,
                venta.Total
            })
            .ToListAsync(ct);

        var registradas = filas.Where(x => x.Estado == EstadoVenta.Registrada).ToArray();
        var anuladas = filas.Where(x => x.Estado == EstadoVenta.Anulada).ToArray();

        var porVendedor = registradas
            .GroupBy(x => new { x.EmpleadoId, x.Vendedor })
            .Select(g => new CierreVendedor(
                g.Key.EmpleadoId,
                g.Key.Vendedor.Trim(),
                g.Count(),
                g.Sum(x => x.Total)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.Ordinal)
            .ToArray();

        return new CierreCaja(
            fecha,
            registradas.Length,
            registradas.Sum(x => x.Subtotal),
            registradas.Sum(x => x.Descuento),
            registradas.Sum(x => x.Total),
            anuladas.Length,
            anuladas.Sum(x => x.Total),
            porVendedor);
    }

    public async Task<IReadOnlyList<ComprasPorProveedor>> ComprasPorProveedor(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct)
    {
        var lineas = await (
            from compra in db.Set<Compra>().AsNoTracking()
            join proveedor in db.Set<Proveedor>().AsNoTracking()
                on compra.ProveedorId equals proveedor.Id
            from linea in compra.Lineas
            where compra.Estado == EstadoCompra.Recibida
                && compra.Fecha >= desdeUtc
                && compra.Fecha < hastaUtc
            select new
            {
                CompraId = compra.Id,
                compra.ProveedorId,
                Proveedor = proveedor.Nombre,
                proveedor.Pais,
                linea.Cantidad,
                linea.CostoUnitarioUsd,
                linea.MercaderiaPen,
                linea.FletePen,
                linea.AduanaPen
            })
            .ToListAsync(ct);

        return lineas
            .GroupBy(x => new { x.ProveedorId, x.Proveedor, x.Pais })
            .Select(g => new ComprasPorProveedor(
                g.Key.ProveedorId,
                g.Key.Proveedor,
                g.Key.Pais,
                g.Select(x => x.CompraId).Distinct().Count(),
                decimal.Round(g.Sum(x => x.Cantidad * x.CostoUnitarioUsd), 2, MidpointRounding.AwayFromZero),
                g.Sum(x => x.MercaderiaPen + x.FletePen + x.AduanaPen)))
            .OrderByDescending(x => x.TotalPen)
            .ThenBy(x => x.Proveedor, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record LineaVendida(
        int ProductoId,
        string Codigo,
        string Nombre,
        string UnidadMedida,
        decimal CostoPromedio,
        decimal Cantidad,
        decimal Subtotal);
}
