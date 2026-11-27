using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record LoginQuery(string Email, string Password) : IRequest<TokenDto>;

internal sealed class LoginQueryHandler(IUsuarioRepository usuarios, IJwtService jwt)
    : IRequestHandler<LoginQuery, TokenDto>
{
    public async Task<TokenDto> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254)
            throw new AccesoNoPermitidoException(401);

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
            throw new AccesoNoPermitidoException(401);

        var usuario = await usuarios.AutenticarAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (usuario == null)
            throw new AccesoNoPermitidoException(401);

        return jwt.Emitir(usuario);
    }
}
