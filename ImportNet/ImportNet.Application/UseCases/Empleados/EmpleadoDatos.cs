using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Empleados;

public sealed record EmpleadoDatos(string Nombre, string Apellidos, string DocumentoIdentidad, string Email, string Telefono,
    string Cargo, string Area);

public sealed record EmpleadoDto(int Id, string UsuarioId, string Nombre, string Apellidos, string? DocumentoIdentidad,
    string Email, string Telefono, string Cargo, string Area, bool Activo, IReadOnlyList<string> Roles);

public interface IAdministracionEmpleados
{
    Task<IReadOnlyList<EmpleadoDto>> Listar(string? busqueda, bool? activo, CancellationToken ct);
    Task<EmpleadoDto?> Obtener(int id, CancellationToken ct);
    Task<int> Registrar(EmpleadoDatos datos, string password, bool activo, string[] roles, CancellationToken ct);
    Task Actualizar(int id, EmpleadoDatos datos, bool activo, string[] roles, CancellationToken ct);
    Task CambiarAcceso(int id, bool? activo, string[]? roles, CancellationToken ct);
}

public static class EmpleadoReglas
{
    public static EmpleadoDatos Validar(EmpleadoDatos? datos)
    {
        if (datos == null)
            throw new ReglaNegocioException("Los datos del empleado son obligatorios.");

        var nombre = Reglas.Texto(datos.Nombre, 200, "Nombre");
        var apellidos = Reglas.Texto(datos.Apellidos, 100, "Apellidos");

        var documento = Reglas.Texto(datos.DocumentoIdentidad, 20, "Documento").ToUpperInvariant();
        if (!Regex.IsMatch(documento, "^[A-Z0-9-]{5,20}$"))
            throw new ReglaNegocioException("Documento: entre 5 y 20 letras, números o guiones.");

        var email = Reglas.Texto(datos.Email, 254, "Correo");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ReglaNegocioException("Correo inválido.");

        var telefono = Reglas.Texto(datos.Telefono, 30, "Teléfono");
        var digitos = telefono.Count(char.IsDigit);
        if (!Regex.IsMatch(telefono, @"^\+?[0-9 ()-]{7,30}$") || digitos < 7 || digitos > 15)
            throw new ReglaNegocioException("Teléfono inválido: indique entre 7 y 15 dígitos.");

        var cargo = Reglas.Texto(datos.Cargo, 200, "Cargo");
        var area = Reglas.Texto(datos.Area, 200, "Área");

        return new EmpleadoDatos(nombre, apellidos, documento, email, telefono, cargo, area);
    }

    public static string[] ValidarRoles(string[]? roles)
    {
        if (roles == null || roles.Length < 1 || roles.Length > RolesImportNet.Todos.Count)
            throw new ReglaNegocioException("Seleccione Administrador o Vendedor.");

        if (roles.Any(r => !RolesImportNet.Todos.Contains(r)))
            throw new ReglaNegocioException("Seleccione Administrador o Vendedor.");

        return roles.Distinct().ToArray();
    }

    public static void ValidarId(int id)
    {
        if (id <= 0)
            throw new ReglaNegocioException("Identificador inválido.");
    }
}
