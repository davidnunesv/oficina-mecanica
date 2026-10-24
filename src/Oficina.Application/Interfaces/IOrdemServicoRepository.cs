
using Oficina.Domain.Entities;

namespace Oficina.Application.Interfaces;

public interface IOrdemServicoRepository
{
    Task CreateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
    Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrdemServico>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
}
