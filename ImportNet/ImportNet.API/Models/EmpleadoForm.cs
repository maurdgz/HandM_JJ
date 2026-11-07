using System.ComponentModel.DataAnnotations;
using ImportNet.Application.UseCases.Empleados;

namespace ImportNet.API.Models;

public sealed class EmpleadoForm
{
    public int Id { get; set; }
    public string? UsuarioId { get; set; }
    [Required, StringLength(200)] public string Nombre { get; set; } = "";
    [Required, StringLength(100)] public string Apellidos { get; set; } = "";
    [Required, StringLength(20)] public string DocumentoIdentidad { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Telefono { get; set; } = "";
    [Required, StringLength(200)] public string Cargo { get; set; } = "";
    [Required, StringLength(200)] public string Area { get; set; } = "";
    public bool Activo { get; set; } = true;
    [MinLength(1)] public string[] Roles { get; set; } = [];
    [DataType(DataType.Password), StringLength(128)] public string? Password { get; set; }
    public EmpleadoDatos Datos() => new(Nombre, Apellidos, DocumentoIdentidad, Email, Telefono, Cargo, Area);
}

public sealed record EmpleadoListado(IReadOnlyList<EmpleadoDto> Empleados, string? Busqueda, bool? Activo);
