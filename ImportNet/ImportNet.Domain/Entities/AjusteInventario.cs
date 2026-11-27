namespace ImportNet.Domain.Entities;

public enum MotivoAjuste { Merma, Rotura, ConteoFisico, DevolucionProveedor }

public static class AjusteInventario
{
    public const int ObservacionMaxima = 300;
    public const decimal CantidadMaxima = 1000000m;

    public static string Etiqueta(MotivoAjuste motivo)
    {
        switch (motivo)
        {
            case MotivoAjuste.Merma: return "Merma";
            case MotivoAjuste.Rotura: return "Rotura";
            case MotivoAjuste.ConteoFisico: return "Conteo físico";
            case MotivoAjuste.DevolucionProveedor: return "Devolución a proveedor";
            default: return motivo.ToString();
        }
    }

    public static MotivoAjuste Interpretar(string? motivo)
    {
        var nombres = Enum.GetNames<MotivoAjuste>();
        var encontrado = nombres.FirstOrDefault(n => string.Equals(n, motivo?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (encontrado == null)
            throw new ReglaNegocioException("Motivo inválido: use Merma, Rotura, ConteoFisico o DevolucionProveedor.");

        return Enum.Parse<MotivoAjuste>(encontrado);
    }

    public static MovimientoInventario Crear(int productoId, MotivoAjuste motivo, decimal cantidad,
        string? observacion, decimal stockActual, int empleadoId, DateTime fecha)
    {
        if (productoId <= 0 || empleadoId <= 0)
            throw new ReglaNegocioException("Producto o empleado inválido.");

        var esConteo = motivo == MotivoAjuste.ConteoFisico;

        if (esConteo && cantidad < 0)
            throw new ReglaNegocioException("La cantidad contada debe ser cero o positiva, con hasta tres decimales.");

        if (!esConteo && cantidad <= 0)
            throw new ReglaNegocioException("La cantidad a descontar debe ser positiva, con hasta tres decimales.");

        if (cantidad > CantidadMaxima || decimal.Round(cantidad, 3) != cantidad)
            throw new ReglaNegocioException(esConteo
                ? "La cantidad contada debe ser cero o positiva, con hasta tres decimales."
                : "La cantidad a descontar debe ser positiva, con hasta tres decimales.");

        var diferencia = esConteo ? cantidad - stockActual : -cantidad;

        if (diferencia == 0)
            throw new ReglaNegocioException("El conteo coincide con el stock actual; no hay ajuste que registrar.");

        if (stockActual + diferencia < 0)
            throw new ReglaNegocioException($"El ajuste dejaría stock negativo: stock actual {stockActual:0.###}.");

        string? texto = null;
        if (!string.IsNullOrWhiteSpace(observacion))
            texto = observacion.Trim();

        if (texto != null && texto.Length > ObservacionMaxima)
            throw new ReglaNegocioException($"La observación admite hasta {ObservacionMaxima} caracteres.");

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
