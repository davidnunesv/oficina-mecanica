
using Oficina.Domain.Entities;

namespace Oficina.Application.Interfaces;

public interface IServicoRepository
{
    Task CreateAsync(Servico servico, CancellationToken cancellationToken = default);
    Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Servico>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default);
    Task DeleteAsync(Servico servico, CancellationToken cancellationToken = default);
    Task<bool> HasDependenciesAsync(Guid servicoId, CancellationToken cancellationToken = default);
}
