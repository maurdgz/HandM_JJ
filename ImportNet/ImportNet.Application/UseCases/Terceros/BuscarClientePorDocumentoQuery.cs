using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record BuscarClientePorDocumentoQuery(string Documento) : IRequest<Cliente>;

internal sealed class BuscarClientePorDocumentoQueryHandler : IRequestHandler<BuscarClientePorDocumentoQuery, Cliente>
{
    private readonly IUnitOfWork _unitOfWork;

    public BuscarClientePorDocumentoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Cliente> Handle(BuscarClientePorDocumentoQuery request, CancellationToken cancellationToken)
    {
        var documento = request.Documento;
        if (documento == null || (documento.Length != 8 && documento.Length != 11) || !documento.All(char.IsAsciiDigit))
            throw new ReglaNegocioException("Documento debe contener 8 u 11 digitos.");

        var cliente = await _unitOfWork.Clientes.BuscarPorDocumento(documento, cancellationToken);
        if (cliente == null)
            throw new NoEncontradoException("Cliente no encontrado.");

        return cliente;
    }
}
