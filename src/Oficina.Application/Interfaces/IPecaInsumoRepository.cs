
using Oficina.Domain.Entities;

namespace Oficina.Application.Interfaces;

public interface IPecaInsumoRepository
{
    Task CreateAsync(PecaInsumo peca, CancellationToken cancellationToken = default);
    Task<PecaInsumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PecaInsumo>> ListAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(PecaInsumo peca, CancellationToken cancellationToken = default);
    Task DeleteAsync(PecaInsumo peca, CancellationToken cancellationToken = default);
    Task<bool> HasDependenciesAsync(Guid pecaId, CancellationToken cancellationToken = default);
}
