using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryDelegatedRole
    {
        public QueryDelegatedRole(DbDelegatedRole role)
        {
            if (role.Person == null)
                throw new ArgumentNullException("role.Person", "Person must be included");
            if (role.CreatedBy == null)
                throw new ArgumentNullException("role.CreatedBy", "CreatedBy person must be included");
            if (role.Contract == null)
                throw new ArgumentNullException("role.Contract", "Contract must be included");
            if (role.Project == null)
                throw new ArgumentNullException("role.Project", "Project must be included");

            Id = role.Id;

            Classification = role.Classification;
            Type = role.Type;
            Person = new QueryPerson(role.Person);
            CreatedBy = new QueryPerson(role.CreatedBy);

            if (role.RecertifiedBy != null)
                RecertifiedBy = new QueryPerson(role.RecertifiedBy);

            Created = role.Created;
            RecertifiedDate = role.RecertifiedDate;
            ValidTo = role.ValidTo;

            Project = new QueryProject(role.Project);
            Contract = new QueryContract(role.Contract);
        }

        public Guid Id { get; set; }

        public DbDelegatedRoleClassification Classification { get; set; }
        public DbDelegatedRoleType Type { get; set; }

        public QueryPerson Person { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? RecertifiedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset? RecertifiedDate { get; set; }

        public QueryProject Project { get; set; }
        public QueryContract Contract { get; set; }

    }
}
