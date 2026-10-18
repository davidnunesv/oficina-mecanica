
using Oficina.Application.DTOs.Clientes;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Application.Services;

public class ClienteApplicationService(IClienteRepository clienteRepository)
{
    private readonly IClienteRepository _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));

    public async Task<Guid> CreateAsync(string nome, string numeroDocumento, CancellationToken cancellationToken = default)
    {
        var documento = new Documento(numeroDocumento);
        var cliente = new Cliente(nome, documento);

        if (await _clienteRepository.HasDocumentoAsync(documento, cancellationToken))
            throw new InvalidOperationException("Já existe um cliente cadastrado com esse CPF/CNPJ.");

        await _clienteRepository.CreateAsync(cliente, cancellationToken);

        return cliente.Id;
    }

    public async Task<ClienteDto?> GetByIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("O ID do cliente é obrigatório.", nameof(clienteId));

        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            return null;

        return new ClienteDto(cliente.Id, cliente.Nome, cliente.Documento.Valor, cliente.Documento.Tipo.ToString());
    }

    public async Task<IReadOnlyList<ClienteDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var clientes = await _clienteRepository.ListAsync(cancellationToken);

        return clientes.Select(cliente => new ClienteDto(
            cliente.Id, cliente.Nome, cliente.Documento.Valor, cliente.Documento.Tipo.ToString()
        )).ToList();
    }


    public async Task<bool> UpdateAsync(Guid clienteId, string nome, string numeroDocumento, CancellationToken cancellationToken = default)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("O ID do cliente é obrigatório.", nameof(clienteId));

        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            return false;

        var documento = new Documento(numeroDocumento);

        if (cliente.Documento != documento && await _clienteRepository.HasDocumentoAsync(documento, cancellationToken))
            throw new InvalidOperationException("Já existe outro cliente cadastrado com esse CPF/CNPJ.");

        cliente.Update(nome, documento);

        await _clienteRepository.UpdateAsync(cliente, cancellationToken);

        return true;
    }


    public async Task<bool>DeleteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("O ID do cliente é obrigatório.", nameof(clienteId));

        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            return false;

        if (await _clienteRepository.PossuiVinculosAsync(clienteId, cancellationToken))
            throw new InvalidOperationException("Não é possível excluir um cliente que possui veículos ou ordens de serviço vinculados.");

        await _clienteRepository.DeleteAsync(cliente, cancellationToken);

        return true;
    }



}
