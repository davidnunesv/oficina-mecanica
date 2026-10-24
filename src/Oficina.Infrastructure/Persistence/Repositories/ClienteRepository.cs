
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository(OficinaDbContext context) : IClienteRepository
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task CreateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasDocumentoAsync(Documento documento, CancellationToken cancellationToken = default)
    {
        return _context.Clientes.AnyAsync(c => c.Documento == documento, cancellationToken);
    }

    public async Task<bool> HasDependenciesAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        if (await _context.Veiculos.AnyAsync(v => v.ClienteId == clienteId, cancellationToken))
            return true;

        return await _context.OrdensServico.AnyAsync(os => os.ClienteId == clienteId, cancellationToken);
    }

    public async Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
