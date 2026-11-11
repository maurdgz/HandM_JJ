using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Auth;

public sealed record GetUsuarioActualQuery : IRequest<UsuarioDto>;
internal sealed class GetUsuarioActualQueryHandler(IUsuarioActual actor, IUsuarioRepository usuarios)
    : IRequestHandler<GetUsuarioActualQuery, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(GetUsuarioActualQuery r, CancellationToken ct)
    {
        var usuario = actor.Id is { } id ? await usuarios.Obtener(id, ct) : null;
        if (usuario is null) throw new AccesoNoPermitidoException(401);
        return UsuarioDto.Desde(usuario);
    }
}
