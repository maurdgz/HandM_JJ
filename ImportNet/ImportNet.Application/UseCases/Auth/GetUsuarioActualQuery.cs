using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record GetUsuarioActualQuery : IRequest<UsuarioDto>;

internal sealed class GetUsuarioActualQueryHandler : IRequestHandler<GetUsuarioActualQuery, UsuarioDto>
{
    private readonly IUsuarioActual _actor;
    private readonly IUsuarioRepository _usuarios;

    public GetUsuarioActualQueryHandler(IUsuarioActual actor, IUsuarioRepository usuarios)
    {
        _actor = actor;
        _usuarios = usuarios;
    }

    public async Task<UsuarioDto> Handle(GetUsuarioActualQuery request, CancellationToken cancellationToken)
    {
        if (_actor.Id == null)
            throw new AccesoNoPermitidoException(401);

        var usuario = await _usuarios.Obtener(_actor.Id, cancellationToken);
        if (usuario == null)
            throw new AccesoNoPermitidoException(401);

        return UsuarioDto.Desde(usuario);
    }
}
