using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Database
{
    public class ResourcesDbContext : DbContext
    {
        public ResourcesDbContext() { }

        public ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : base(options)
        {
        }

        public DbSet<DbContractPersonnel> ContractPersonnel { get; set; }

        public DbSet<DbPerson> Persons { get; set; }
        public DbSet<DbContract> Contracts { get; set; }
        public DbSet<DbProject> Projects { get; set; }
        public DbSet<DbContractorRequest> ContractorRequests { get; set; }

        public DbSet<DbExternalPersonnelPerson> ExternalPersonnel { get; set; }


        public bool HasUnsavedChanges()
        {
            return this.ChangeTracker.Entries().Any(e => e.State == EntityState.Added
                                                         || e.State == EntityState.Modified
                                                         || e.State == EntityState.Deleted);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbContractPersonnel.OnModelCreating(modelBuilder);
            DbExternalPersonnelPerson.OnModelCreating(modelBuilder);
            DbContractorRequest.OnModelCreating(modelBuilder);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            base.OnConfiguring(optionsBuilder);
        }
    }

    public interface ITransactionScope
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

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
