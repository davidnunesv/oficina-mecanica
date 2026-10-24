
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class OrdemServicoRepository(OficinaDbContext context) : IOrdemServicoRepository
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task CreateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        await _context.OrdensServico.AddAsync(ordemServico, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.OrdensServico
            .Include(os => os.Diagnostico)
            .Include(os => os.Orcamento)
            .Include(os => os.ItensServico)
            .Include(os => os.ItensPeca)
            .AsSplitQuery()
            .FirstOrDefaultAsync(os => os.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrdemServico>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrdensServico
            .AsNoTracking()
            .Include(os => os.Diagnostico)
            .Include(os => os.Orcamento)
            .Include(os => os.ItensServico)
            .Include(os => os.ItensPeca)
            .AsSplitQuery()
            .OrderByDescending(os => os.DataInicioExecucao)
            .ThenBy(os => os.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
