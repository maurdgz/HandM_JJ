namespace ImportNet.Domain.Entities;

// Account snapshots decouple the application from the retained Identity storage.
public sealed record Rol(string Id, string Nombre);
public sealed record UsuarioRol(string UsuarioId, Rol Rol);
public sealed record Usuario(string Id, string Email, string VersionAcceso, Empleado Empleado,
    IReadOnlyList<UsuarioRol> Roles);
