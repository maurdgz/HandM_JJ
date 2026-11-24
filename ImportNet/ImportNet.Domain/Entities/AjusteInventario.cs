namespace ImportNet.Domain.Entities;

public enum MotivoAjuste { Merma, Rotura, ConteoFisico, DevolucionProveedor }

// Un ajuste nunca corrige un movimiento anterior: agrega uno nuevo que compensa la diferencia.
public static class AjusteInventario
{
    public const int ObservacionMaxima = 300;
    public const decimal CantidadMaxima = 1000000m;

    public static string Etiqueta(MotivoAjuste motivo) => motivo switch
    {
        MotivoAjuste.Merma => "Merma",
        MotivoAjuste.Rotura => "Rotura",
        MotivoAjuste.ConteoFisico => "Conteo físico",
        MotivoAjuste.DevolucionProveedor => "Devolución a proveedor",
        _ => motivo.ToString()
    };

    public static MotivoAjuste Interpretar(string? motivo)
    {
        if (!string.IsNullOrWhiteSpace(motivo)
            && Enum.TryParse<MotivoAjuste>(motivo.Trim(), ignoreCase: true, out var valor)
            && Enum.IsDefined(valor)
            && !int.TryParse(motivo, out _))
            return valor;

        throw new ReglaNegocioException(
            "Motivo inválido: use Merma, Rotura, ConteoFisico o DevolucionProveedor.");
    }

    /// <summary>
    /// Para ConteoFisico, <paramref name="cantidad"/> es lo contado en el almacén y se registra la diferencia.
    /// Para los demás motivos es la cantidad que sale del stock.
    /// </summary>
    public static MovimientoInventario Crear(
        int productoId,
        MotivoAjuste motivo,
        decimal cantidad,
        string? observacion,
        decimal stockActual,
        int empleadoId,
        DateTime fecha)
    {
        if (productoId <= 0 || empleadoId <= 0)
            throw new ReglaNegocioException("Producto o empleado inválido.");

        if (!Enum.IsDefined(motivo))
            throw new ReglaNegocioException("Motivo de ajuste inválido.");

        var esConteo = motivo == MotivoAjuste.ConteoFisico;

        if ((esConteo ? cantidad < 0 : cantidad <= 0)
            || cantidad > CantidadMaxima
            || decimal.Round(cantidad, 3) != cantidad)
            throw new ReglaNegocioException(esConteo
                ? "La cantidad contada debe ser cero o positiva, con hasta tres decimales."
                : "La cantidad a descontar debe ser positiva, con hasta tres decimales.");

        var diferencia = esConteo ? cantidad - stockActual : -cantidad;

        if (diferencia == 0)
            throw new ReglaNegocioException(
                "El conteo coincide con el stock actual; no hay ajuste que registrar.");

        if (stockActual + diferencia < 0)
            throw new ReglaNegocioException(
                $"El ajuste dejaría stock negativo: stock actual {stockActual:0.###}.");

        var texto = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();

        if (texto?.Length > ObservacionMaxima)
            throw new ReglaNegocioException(
                $"La observación admite hasta {ObservacionMaxima} caracteres.");

        return new MovimientoInventario
        {
            ProductoId = productoId,
            AlmacenId = Almacen.PrincipalId,
            Tipo = TipoMovimiento.Ajuste,
            Cantidad = diferencia,
            Fecha = fecha,
            EmpleadoId = empleadoId,
            MotivoAjuste = motivo,
            Observacion = texto
        };
    }
}
