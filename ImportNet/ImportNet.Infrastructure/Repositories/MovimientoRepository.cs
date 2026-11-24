using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class MovimientoRepository(
    AppDbContext context) : IMovimientoRepository
{
    public void Agregar(MovimientoInventario movimiento)
    {
        context.Set<MovimientoInventario>().Add(movimiento);
    }

    public Task<bool> TieneEntradasDeCompra(
        int compraId,
        CancellationToken ct)
    {
        return context.Set<MovimientoInventario>()
            .AnyAsync(
                x => x.CompraId == compraId
                    && x.Tipo == TipoMovimiento.Entrada,
                ct);
    }

    public async Task<IReadOnlyList<MovimientoInventario>>
        ListarPorCompra(
            int compraId,
            CancellationToken ct)
    {
        return await context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Where(x => x.CompraId == compraId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<KardexProducto?> Kardex(
        int productoId,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken ct)
    {
        var producto = await context.Set<Producto>()
            .AsNoTracking()
            .Where(x => x.Id == productoId)
            .Select(x => new { x.Id, x.Codigo, x.Nombre, x.UnidadMedida })
            .SingleOrDefaultAsync(ct);

        if (producto is null)
            return null;

        var stockActual = await context.Set<Inventario>()
            .AsNoTracking()
            .Where(x => x.ProductoId == productoId
                && x.AlmacenId == Almacen.PrincipalId)
            .Select(x => (decimal?)x.Cantidad)
            .SingleOrDefaultAsync(ct) ?? 0m;

        var consulta =
            from movimiento in context.Set<MovimientoInventario>().AsNoTracking()
            join empleado in context.Set<Empleado>().AsNoTracking()
                on movimiento.EmpleadoId equals empleado.Id
            join venta in context.Set<Venta>().AsNoTracking()
                on movimiento.VentaId equals (int?)venta.Id into ventas
            from venta in ventas.DefaultIfEmpty()
            where movimiento.ProductoId == productoId
                && movimiento.AlmacenId == Almacen.PrincipalId
            select new
            {
                movimiento.Id,
                movimiento.Fecha,
                movimiento.Tipo,
                movimiento.Cantidad,
                movimiento.CompraId,
                movimiento.VentaId,
                VentaNumero = venta == null ? (int?)null : venta.Numero,
                movimiento.MotivoAjuste,
                movimiento.Observacion,
                Empleado = empleado.Nombre + " " + empleado.Apellidos
            };

        if (hastaUtc is { } hasta)
            consulta = consulta.Where(x => x.Fecha < hasta);

        // SQLite no suma decimales en servidor; el saldo acumulado se calcula sobre la lista ordenada.
        var filas = await consulta
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var conSigno = filas
            .Select(x => new
            {
                Fila = x,
                Cantidad = MovimientoInventario.ConSigno(x.Tipo, x.Cantidad)
            })
            .ToList();

        var saldoInicial = desdeUtc is { } desde
            ? conSigno.Where(x => x.Fila.Fecha < desde).Sum(x => x.Cantidad)
            : 0m;

        var enRango = desdeUtc is { } inicio
            ? conSigno.Where(x => x.Fila.Fecha >= inicio)
            : conSigno;

        var saldo = saldoInicial;
        var lineas = new List<LineaKardex>();

        foreach (var item in enRango)
        {
            var fila = item.Fila;
            saldo += item.Cantidad;

            lineas.Add(new LineaKardex(
                fila.Id,
                fila.Fecha,
                fila.Tipo.ToString(),
                Referencia(fila.Tipo, fila.CompraId, fila.VentaNumero, fila.MotivoAjuste),
                fila.CompraId,
                fila.VentaId,
                item.Cantidad > 0 ? item.Cantidad : 0m,
                item.Cantidad < 0 ? -item.Cantidad : 0m,
                saldo,
                fila.Empleado.Trim(),
                fila.MotivoAjuste is { } motivo ? AjusteInventario.Etiqueta(motivo) : null,
                fila.Observacion));
        }

        return new KardexProducto(
            producto.Id,
            producto.Codigo,
            producto.Nombre,
            producto.UnidadMedida,
            saldoInicial,
            lineas.Sum(x => x.Entrada),
            lineas.Sum(x => x.Salida),
            saldo,
            stockActual,
            lineas);
    }

    private static string Referencia(
        TipoMovimiento tipo,
        int? compraId,
        int? ventaNumero,
        MotivoAjuste? motivo)
    {
        if (compraId is { } compra)
            return $"Compra #{compra}";

        if (ventaNumero is { } venta)
            return $"Venta N° {venta}";

        if (tipo == TipoMovimiento.Ajuste && motivo is { } ajuste)
            return $"Ajuste: {AjusteInventario.Etiqueta(ajuste)}";

        return tipo.ToString();
    }
}