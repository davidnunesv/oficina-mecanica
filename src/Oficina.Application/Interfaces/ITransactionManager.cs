
namespace Oficina.Application.Interfaces;

public interface ITransactionManager
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
