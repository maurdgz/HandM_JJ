using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Cliente : Entity
{
    public TipoDocumento TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
}
