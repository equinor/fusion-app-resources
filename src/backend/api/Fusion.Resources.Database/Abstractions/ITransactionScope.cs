using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Fusion.Resources.Database
{
    public interface ITransactionScope
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
