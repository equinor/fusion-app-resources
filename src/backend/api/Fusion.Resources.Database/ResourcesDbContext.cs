using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
#nullable disable

namespace Fusion.Resources.Database
{
    public class ResourcesDbContext : DbContext
    {
        private readonly ISqlAuthenticationManager authManager;

        public ResourcesDbContext()
        { }

        public ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : base(options)
        {
        }

        public ResourcesDbContext(DbContextOptions<ResourcesDbContext> options, ISqlAuthenticationManager authManager) : base(options)
        {
            this.authManager = authManager;
        }

        public DbSet<DbPerson> Persons { get; set; }
        public DbSet<DbContract> Contracts { get; set; }
        public DbSet<DbProject> Projects { get; set; }
        public DbSet<DbRequestComment> RequestComments { get; set; }
        public DbSet<DbResourceAllocationRequest> ResourceAllocationRequests { get; set; }
        public DbSet<DbWorkflow> Workflows { get; set; }
        public DbSet<DbResponsibilityMatrix> ResponsibilityMatrices { get; set; }
        public DbSet<DbPersonAbsence> PersonAbsences { get; set; }
        public DbSet<DbPersonNote> PersonNotes { get; set; }
        public DbSet<DbRequestAction> RequestActions { get; set; }
        public DbSet<DbConversationMessage> RequestConversations { get; set; }
        public DbSet<DbSharedRequest> SharedRequests { get; set; }
        public DbSet<DbSecondOpinionPrompt> SecondOpinions { get; set; }
        public DbSet<DbSecondOpinionResponse> SecondOpinionResponses { get; set; }
        public DbSet<DbDelegatedDepartmentResponsible> DelegatedDepartmentResponsibles { get; set; }


        #region Moved to Contract Personnel. Only here for historical reasons
        public DbSet<DbContractPersonnelReplacement> ContractPersonnelReplacementChanges { get; set; }
        public DbSet<DbContractPersonnel> ContractPersonnel { get; set; }
        public DbSet<DbContractorRequest> ContractorRequests { get; set; }
        public DbSet<DbExternalPersonnelPerson> ExternalPersonnel { get; set; }
        public DbSet<DbDelegatedRole> DelegatedRoles { get; set; }
        #endregion


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbWorkflow.OnModelCreating(modelBuilder);
            DbWorkflowStep.OnModelCreating(modelBuilder);
            DbRequestComment.OnModelCreating(modelBuilder);
            DbPerson.OnModelCreating(modelBuilder);
            DbPersonAbsence.OnModelCreating(modelBuilder);
            DbPersonNote.OnModelCreating(modelBuilder);
            DbResponsibilityMatrix.OnModelCreating(modelBuilder);
            DbResourceAllocationRequest.OnModelCreating(modelBuilder);
            DbRequestAction.OnModelCreating(modelBuilder);
            DbConversationMessage.OnModelCreating(modelBuilder);
            DbContractPersonnelReplacement.OnModelCreating(modelBuilder);
            DbSharedRequest.OnModelCreating(modelBuilder);
            DbSecondOpinionPrompt.OnModelCreating(modelBuilder);
            DbSecondOpinionResponse.OnModelCreating(modelBuilder);

            #region Moved to Contract Personnel. Only here for historical reasons
            DbContractPersonnel.OnModelCreating(modelBuilder);
            DbContractorRequest.OnModelCreating(modelBuilder);
            DbExternalPersonnelPerson.OnModelCreating(modelBuilder);
            DbDelegatedRole.OnModelCreating(modelBuilder);
            DbDelegatedDepartmentResponsible.OnModelCreating(modelBuilder);
            #endregion
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

#nullable enable