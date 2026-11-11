using ImportNet.Domain.Entities;

namespace ImportNet.Application.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}

public interface IUsuarioRepository
{
    Task<Usuario?> Autenticar(string email, string password, CancellationToken ct);
    Task<Usuario?> Obtener(string id, CancellationToken ct);
}

public interface IJwtService
{
    TokenDto Emitir(Usuario usuario);
}

public sealed record TokenDto(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
public sealed record UsuarioDto(string Id, string Email, int EmpleadoId, string Nombre, string Apellidos,
    IReadOnlyList<string> Roles)
{
    public static UsuarioDto Desde(Usuario usuario) => new(usuario.Id, usuario.Email, usuario.Empleado.Id,
        usuario.Empleado.Nombre, usuario.Empleado.Apellidos, usuario.Roles.Select(r => r.Rol.Nombre).Order().ToArray());
}
