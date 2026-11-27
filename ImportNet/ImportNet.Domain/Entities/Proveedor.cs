using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Proveedor : Entity
{
    public string Nombre { get; set; } = "";
    public TipoProveedor Tipo { get; set; }
    public string Pais { get; set; } = "";
    public string Contacto { get; set; } = "";
    public bool Activo { get; set; } = true;

    public List<ProveedorProducto> Suministros { get; set; } = [];
}
