
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Application.Interfaces;

public interface IClienteRepository
{
    Task<bool> HasDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);
    Task CreateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task<bool> PossuiVinculosAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
