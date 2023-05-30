using System.Threading.Tasks;
using System.Threading;
using System;

namespace Fusion.Resources.Api
{
    /// <summary>
    /// Transaction that wraps multiple data sources. 
    /// 
    /// There are events dispatched when commands are executed, which are published immediatly when data is saved.
    /// This means external clients are aware of changes, however these might not have been commited to the database if there are 
    /// long tasks to perform before data is commited.
    /// </summary>
    public interface IUnifiedTransaction : IAsyncDisposable // Only implement async disposable, as one downstream source only support async
    {
        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
