
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class VeiculoRepository(OficinaDbContext context) : IVeiculoRepository
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task CreateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
    {
        await _context.Veiculos.AddAsync(veiculo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Veiculos.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Veiculo>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Veiculos
            .AsNoTracking()
            .OrderBy(v => v.Marca)
            .ThenBy(v => v.Modelo)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasPlacaAsync(Placa placa, CancellationToken cancellationToken = default)
    {
        return _context.Veiculos.AnyAsync(v => v.Placa == placa, cancellationToken);
    }

    public Task<bool> HasDependenciesAsync(Guid veiculoId, CancellationToken cancellationToken = default)
    {
        return _context.OrdensServico.AnyAsync(os => os.VeiculoId == veiculoId, cancellationToken);
    }

    public async Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
    {
        _context.Veiculos.Remove(veiculo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
