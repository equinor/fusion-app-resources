using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;

#nullable disable

namespace Fusion.Resources.Database
{
    public class ResourcesDbContext : DbContext
    {
        private readonly ISqlAuthenticationManager authManager;

        public ResourcesDbContext() { }

        public ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : base(options)
        {
        }

        public ResourcesDbContext(DbContextOptions<ResourcesDbContext> options, ISqlAuthenticationManager authManager) : base(options)
        {
            this.authManager = authManager;
        }

        public DbSet<DbContractPersonnel> ContractPersonnel { get; set; }

        public DbSet<DbPerson> Persons { get; set; }
        public DbSet<DbContract> Contracts { get; set; }
        public DbSet<DbProject> Projects { get; set; }
        public DbSet<DbContractorRequest> ContractorRequests { get; set; }
        public DbSet<DbRequestComment> RequestComments { get; set; }
        public DbSet<DbResourceAllocationRequest> ResourceAllocationRequests { get; set; }

        public DbSet<DbExternalPersonnelPerson> ExternalPersonnel { get; set; }

        public DbSet<DbWorkflow> Workflows { get; set; }

        public DbSet<DbDelegatedRole> DelegatedRoles { get; set; }

        public DbSet<DbResponsibilityMatrix> ResponsibilityMatrices { get; set; }
        public DbSet<DbPersonAbsence> PersonAbsences { get; set; }
        public DbSet<DbPersonNote> PersonNotes { get; set; }

        public DbSet<DbDepartment> Departments { get; set; }
        public DbSet<DbDepartmentResponsible> DepartmentResponsibles { get; set; }

        public DbSet<DbRequestTask> RequestTasks { get; set; }
        public DbSet<DbConversationMessage> RequestConversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbContractPersonnel.OnModelCreating(modelBuilder);
            DbExternalPersonnelPerson.OnModelCreating(modelBuilder);
            DbContractorRequest.OnModelCreating(modelBuilder);
            DbWorkflow.OnModelCreating(modelBuilder);
            DbWorkflowStep.OnModelCreating(modelBuilder);
            DbRequestComment.OnModelCreating(modelBuilder);
            DbPerson.OnModelCreating(modelBuilder);
            DbDelegatedRole.OnModelCreating(modelBuilder);
            DbPersonAbsence.OnModelCreating(modelBuilder);
            DbPersonNote.OnModelCreating(modelBuilder);
            DbResponsibilityMatrix.OnModelCreating(modelBuilder);
            DbResourceAllocationRequest.OnModelCreating(modelBuilder);
            DbDepartment.OnModelCreating(modelBuilder);
            DbDepartmentResponsible.OnModelCreating(modelBuilder);
            DbRequestTask.OnModelCreating(modelBuilder);
            DbConversationMessage.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (authManager != null)
            {
                var connection = authManager.GetSqlConnection();
                optionsBuilder.UseSqlServer(connection);
            }

            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }


    }
}
