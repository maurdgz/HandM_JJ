using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record BuscarClientePorDocumentoQuery(string Documento) : IRequest<Cliente>;

internal sealed class BuscarClientePorDocumentoQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<BuscarClientePorDocumentoQuery, Cliente>
{
    public async Task<Cliente> Handle(BuscarClientePorDocumentoQuery request, CancellationToken cancellationToken)
    {
        var documento = request.Documento;
        if (documento == null || (documento.Length != 8 && documento.Length != 11) || !documento.All(char.IsAsciiDigit))
            throw new ReglaNegocioException("Documento debe contener 8 u 11 digitos.");

        return await unitOfWork.Clientes.BuscarPorDocumentoAsync(documento, cancellationToken)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
    }
}
