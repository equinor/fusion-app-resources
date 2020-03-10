using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Fusion.Resources.Database
{
    public class EFTransactionScope : ITransactionScope
    {
        private readonly ResourcesDbContext db;

        public EFTransactionScope(ResourcesDbContext db)
        {
            this.db = db;
        }
        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted);
        }
    }
}
