
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Application.Interfaces;

public interface IVeiculoRepository
{
    Task<bool> HasPlacaAsync(Placa placa, CancellationToken cancellationToken = default);
    Task CreateAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
    Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Veiculo>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
    Task<bool> HasDependenciesAsync(Guid veiculoId, CancellationToken cancellationToken = default);
}
