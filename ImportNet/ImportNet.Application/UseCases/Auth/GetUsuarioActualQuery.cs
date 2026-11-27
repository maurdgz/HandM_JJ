using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record GetUsuarioActualQuery : IRequest<UsuarioDto>;

internal sealed class GetUsuarioActualQueryHandler(IUsuarioActual actor, IUsuarioRepository usuarios)
    : IRequestHandler<GetUsuarioActualQuery, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(GetUsuarioActualQuery request, CancellationToken cancellationToken)
    {
        if (actor.Id == null)
            throw new AccesoNoPermitidoException(401);

        var usuario = await usuarios.ObtenerAsync(actor.Id, cancellationToken);
        if (usuario == null)
            throw new AccesoNoPermitidoException(401);

        return UsuarioDto.Desde(usuario);
    }
}
