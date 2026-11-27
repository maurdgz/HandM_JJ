namespace ImportNet.Domain.Entities;

public sealed record Usuario(string Id, string Email, string VersionAcceso, Empleado Empleado, IReadOnlyList<string> Roles);
