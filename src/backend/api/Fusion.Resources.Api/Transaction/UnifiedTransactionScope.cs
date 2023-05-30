using Fusion.Events;
using Fusion.Resources.Database;
using System.Threading.Tasks;

namespace Fusion.Resources.Api
{
    public class UnifiedTransactionScope : IUnifiedTransactionScope
    {
        private readonly IEventNotificationClient eventClient;
        private readonly ITransactionScope transactionScope;

        public UnifiedTransactionScope(IEventNotificationClient eventClient, ITransactionScope transactionScope)
        {
            this.eventClient = eventClient;
            this.transactionScope = transactionScope;
        }

        public async Task<IUnifiedTransaction> BeginTransactionAsync()
        {
            var eventTransaction = await eventClient.BeginTransactionAsync();
            var dbTransaction = await transactionScope.BeginTransactionAsync();

            return new UnifiedTransaction(dbTransaction, eventTransaction);
        }
    }
}
