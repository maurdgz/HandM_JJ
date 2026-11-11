using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record LoginQuery(string Email, string Password) : IRequest<TokenDto>;

internal sealed class LoginQueryHandler(IUsuarioRepository usuarios, IJwtService jwt) : IRequestHandler<LoginQuery, TokenDto>
{
    public async Task<TokenDto> Handle(LoginQuery r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Email) || r.Email.Length > 254 || string.IsNullOrEmpty(r.Password) || r.Password.Length > 1024)
            throw new AccesoNoPermitidoException(401);
        var usuario = await usuarios.Autenticar(r.Email.Trim(), r.Password, ct);
        if (usuario is null) throw new AccesoNoPermitidoException(401);
        return jwt.Emitir(usuario);
    }
}
