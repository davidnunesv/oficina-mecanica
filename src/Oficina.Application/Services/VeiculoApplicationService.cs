
using Oficina.Application.DTOs.Veiculos;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Application.Services;

public class VeiculoApplicationService(IVeiculoRepository veiculoRepository, IClienteRepository clienteRepository)
{
    private readonly IVeiculoRepository _veiculoRepository = veiculoRepository ?? throw new ArgumentNullException(nameof(veiculoRepository));
    private readonly IClienteRepository _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));

    public async Task<Guid> CreateAsync(Guid clienteId, string placa, string marca, string modelo, int ano, CancellationToken cancellationToken = default)
    {
        var veiculo = new Veiculo(clienteId, new Placa(placa), marca, modelo, ano);

        if (await _clienteRepository.GetByIdAsync(clienteId, cancellationToken) is null)
            throw new KeyNotFoundException("O cliente informado não foi encontrado.");

        if (await _veiculoRepository.HasPlacaAsync(veiculo.Placa, cancellationToken))
            throw new InvalidOperationException("Já existe um veículo cadastrado com essa placa.");

        await _veiculoRepository.CreateAsync(veiculo, cancellationToken);

        return veiculo.Id;
    }

    public async Task<VeiculoDto?> GetByIdAsync(Guid veiculoId, CancellationToken cancellationToken = default)
    {
        if (veiculoId == Guid.Empty)
            throw new ArgumentException("O ID do veículo é obrigatório.", nameof(veiculoId));

        var veiculo = await _veiculoRepository.GetByIdAsync(veiculoId, cancellationToken);

        return veiculo is null ? null : MapearParaDto(veiculo);
    }

    public async Task<IReadOnlyList<VeiculoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var veiculos = await _veiculoRepository.ListAsync(cancellationToken);

        return veiculos.Select(MapearParaDto).ToList();
    }

    public async Task<bool> UpdateAsync(Guid veiculoId, string placa, string marca, string modelo, int ano, CancellationToken cancellationToken = default)
    {
        if (veiculoId == Guid.Empty)
            throw new ArgumentException("O ID do veículo é obrigatório.", nameof(veiculoId));

        var veiculo = await _veiculoRepository.GetByIdAsync(veiculoId, cancellationToken);

        if (veiculo is null)
            return false;

        var novaPlaca = new Placa(placa);

        if (veiculo.Placa != novaPlaca && await _veiculoRepository.HasPlacaAsync(novaPlaca, cancellationToken))
            throw new InvalidOperationException("Já existe outro veículo cadastrado com essa placa.");

        veiculo.Update(novaPlaca, marca, modelo, ano);

        await _veiculoRepository.UpdateAsync(veiculo, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid veiculoId, CancellationToken cancellationToken = default)
    {
        if (veiculoId == Guid.Empty)
            throw new ArgumentException("O ID do veículo é obrigatório.", nameof(veiculoId));

        var veiculo = await _veiculoRepository.GetByIdAsync(veiculoId, cancellationToken);

        if (veiculo is null)
            return false;

        if (await _veiculoRepository.HasDependenciesAsync(veiculoId, cancellationToken))
            throw new InvalidOperationException("Não é possível excluir um veículo que possui ordens de serviço vinculadas.");

        await _veiculoRepository.DeleteAsync(veiculo, cancellationToken);

        return true;
    }

    private static VeiculoDto MapearParaDto(Veiculo veiculo)
    {
        return new VeiculoDto(veiculo.Id, veiculo.ClienteId, veiculo.Placa.Valor, veiculo.Marca, veiculo.Modelo, veiculo.Ano);
    }
}
