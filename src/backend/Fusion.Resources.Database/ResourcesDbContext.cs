using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
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

        public async Task SaveChangesAsync()
        {
            var trackables = ChangeTracker.Entries<ITrackableEntity>();

            if (trackables != null)
            {
                // added
                var entityEntries = trackables.ToList();
                foreach (var item in entityEntries.Where(t => t.State == EntityState.Added))
                {
                    if (item.Entity.Created == DateTime.MinValue)
                        item.Entity.Created = DateTimeOffset.UtcNow;

                    //if (userAccessor != null)
                    //{
                    //    item.Entity.CreatedBy = userAccessor.CurrentUser.AzureUniquePersonId;
                    //}
                }
                // modified
                foreach (var item in entityEntries.Where(t => t.State == EntityState.Modified))
                {
                    item.Entity.Updated = DateTimeOffset.UtcNow;
                    //if (userAccessor != null)
                    //{
                    //    item.Entity.ModifiedBy = userAccessor.CurrentUser.AzureUniquePersonId;
                    //}
                }
            }

            try
            {
                if (HasUnsavedChanges())
                    await base.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                //TODO: If we want to we can do more excessive logging and exception handling when concurrency errors occurs.
                //Like:
                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    foreach (var property in proposedValues.Properties)
                    {
                        var proposedValue = proposedValues[property];
                        var databaseValue = databaseValues[property];
                    }
                }

                //But for now:
                throw;
            }

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbContractPersonnel.OnModelCreating(modelBuilder);
            DbExternalPersonnelPerson.OnModelCreating(modelBuilder);
            DbContractorRequest.OnModelCreating(modelBuilder);

        }
    }
}
