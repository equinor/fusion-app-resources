using System.Threading.Tasks;

namespace Fusion.Resources.Api
{
    /// <summary>
    /// Create a transaction scope for multiple sources.
    /// </summary>
    public interface IUnifiedTransactionScope
    {
        Task<IUnifiedTransaction> BeginTransactionAsync();
    }
}
