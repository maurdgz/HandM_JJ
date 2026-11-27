using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record LoginQuery(string Email, string Password) : IRequest<TokenDto>;

internal sealed class LoginQueryHandler : IRequestHandler<LoginQuery, TokenDto>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IJwtService _jwt;

    public LoginQueryHandler(IUsuarioRepository usuarios, IJwtService jwt)
    {
        _usuarios = usuarios;
        _jwt = jwt;
    }

    public async Task<TokenDto> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254)
            throw new AccesoNoPermitidoException(401);

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
            throw new AccesoNoPermitidoException(401);

        var usuario = await _usuarios.Autenticar(request.Email.Trim(), request.Password, cancellationToken);
        if (usuario == null)
            throw new AccesoNoPermitidoException(401);

        return _jwt.Emitir(usuario);
    }
}
