using Fusion.Events;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Fusion.Resources.Api
{
    public class UnifiedTransaction : IUnifiedTransaction, IAsyncDisposable
    {
        private readonly IDbContextTransaction dbTransaction;
        private readonly IEventNotificationTransaction eventTransaction;

        public UnifiedTransaction(IDbContextTransaction dbTransaction, IEventNotificationTransaction eventTransaction)
        {
            this.dbTransaction = dbTransaction;
            this.eventTransaction = eventTransaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await dbTransaction.CommitAsync(cancellationToken);
            await eventTransaction.CommitAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await dbTransaction.DisposeAsync();
            await eventTransaction.DisposeAsync();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            await eventTransaction.RollbackAsync();
        }
    }
}
