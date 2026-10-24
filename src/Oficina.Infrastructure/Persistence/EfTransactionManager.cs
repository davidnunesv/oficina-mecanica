
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Interfaces;

namespace Oficina.Infrastructure.Persistence;

public sealed class EfTransactionManager(OficinaDbContext context) : ITransactionManager
{
    private readonly OficinaDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
