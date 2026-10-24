
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class ServicoRepository(OficinaDbContext context) : IServicoRepository
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task CreateAsync(Servico servico, CancellationToken cancellationToken = default)
    {
        await _context.Servicos.AddAsync(servico, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Servicos.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Servico>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Servicos
            .AsNoTracking()
            .OrderBy(s => s.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasDependenciesAsync(Guid servicoId, CancellationToken cancellationToken = default)
    {
        return _context.Set<ItemServico>()
            .AnyAsync(item => item.ServicoId == servicoId, cancellationToken);
    }

    public async Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Servico servico, CancellationToken cancellationToken = default)
    {
        _context.Servicos.Remove(servico);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
