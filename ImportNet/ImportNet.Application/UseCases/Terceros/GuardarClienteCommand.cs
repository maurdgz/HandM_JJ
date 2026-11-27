using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record GuardarClienteCommand(int Id, TipoDocumento TipoDocumento, string NumeroDocumento, string Nombre, string? Telefono)
    : IRequest<int>;

internal sealed class GuardarClienteCommandHandler : IRequestHandler<GuardarClienteCommand, int>
{
    private const string DocumentoReservado = "00000000";

    private readonly IUnitOfWork _unitOfWork;

    public GuardarClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GuardarClienteCommand request, CancellationToken cancellationToken)
    {
        var documento = request.NumeroDocumento;
        var largo = request.TipoDocumento == TipoDocumento.Dni ? 8 : 11;

        if (!Enum.IsDefined(request.TipoDocumento) || documento == null || documento.Length != largo
            || !documento.All(char.IsAsciiDigit) || documento == DocumentoReservado)
            throw new ReglaNegocioException("DNI de 8 digitos o RUC de 11 digitos requerido; documento reservado no editable.");

        var nombre = Reglas.Texto(request.Nombre, 200, "Nombre");

        if (request.Telefono != null && request.Telefono.Length > 20)
            throw new ReglaNegocioException("Telefono admite 20 caracteres.");

        if (await _unitOfWork.Clientes.DocumentoExiste(documento, request.Id, cancellationToken))
            throw new ConflictoException("Documento duplicado.");

        Cliente cliente;
        if (request.Id == 0)
        {
            cliente = new Cliente();
            _unitOfWork.Clientes.AgregarCliente(cliente);
        }
        else
        {
            cliente = await _unitOfWork.Clientes.Cliente(request.Id, cancellationToken)
                ?? throw new NoEncontradoException("Cliente no encontrado.");

            if (cliente.NumeroDocumento == DocumentoReservado)
                throw new ConflictoException("Cliente varios es reservado.");
        }

        cliente.Nombre = nombre;
        cliente.TipoDocumento = request.TipoDocumento;
        cliente.NumeroDocumento = documento;
        cliente.Telefono = request.Telefono;

        await _unitOfWork.Guardar(cancellationToken);
        return cliente.Id;
    }
}
