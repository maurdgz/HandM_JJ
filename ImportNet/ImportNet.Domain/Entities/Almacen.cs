using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Almacen : Entity
{
    public const int PrincipalId = 1;

    public string Nombre { get; set; } = "";
}
