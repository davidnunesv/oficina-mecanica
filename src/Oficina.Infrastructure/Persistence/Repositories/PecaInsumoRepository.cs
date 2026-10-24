
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class PecaInsumoRepository(OficinaDbContext context) : IPecaInsumoRepository
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task CreateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
    {
        await _context.PecasInsumos.AddAsync(peca, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PecaInsumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.PecasInsumos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PecaInsumo>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PecasInsumos
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasDependenciesAsync(Guid pecaId, CancellationToken cancellationToken = default)
    {
        return _context.Set<ItemPecaOS>()
            .AnyAsync(item => item.PecaInsumoId == pecaId, cancellationToken);
    }

    public async Task UpdateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
    {
        _context.PecasInsumos.Remove(peca);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
