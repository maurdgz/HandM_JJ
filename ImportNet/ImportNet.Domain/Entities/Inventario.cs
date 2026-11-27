using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Inventario : Entity
{
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public decimal Cantidad { get; set; }
    public uint Version { get; set; }

    public Producto Producto { get; set; } = null!;
}
