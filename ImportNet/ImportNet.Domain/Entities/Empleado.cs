using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Empleado : Entity
{
    public string UsuarioId { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? DocumentoIdentidad { get; set; }
    public string Cargo { get; set; } = "";
    public string Area { get; set; } = "";
    public bool Activo { get; set; } = true;
}
