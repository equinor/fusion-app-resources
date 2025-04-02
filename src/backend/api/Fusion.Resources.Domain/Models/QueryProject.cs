using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryProject
    {
        public QueryProject(DbProject project)
        {
            Id = project.Id;
            Name = project.Name;
            DomainId = project.DomainId;
            OrgProjectId = project.OrgProjectId;
            State = project.State.ResolveProjectState();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? DomainId { get; set; }
        public Guid OrgProjectId { get; set; }
        public string? State { get; set; }

    }

    public class QueryProjectRef
    {
        public QueryProjectRef(QueryProject project)
            : this(project.OrgProjectId, project.Name, project.DomainId ?? "", "", project.State)
        {
        }

        /// This constructor will not treat state == null as the project's state being Active. The
        /// project's data may be coming from sources other than the resources db and so resolving
        /// the state is up to the callee.
        public QueryProjectRef(Guid orgId, string name, string domainId, string type, string? state)
        {
            OrgProjectId = orgId;
            Name = name;
            DomainId = domainId;
            Type = type;
            State = state;
        }
        public string Name { get; set; }
        public string? DomainId { get; set; }
        public Guid OrgProjectId { get; set; }
        public string? State { get; set; }
        public string Type { get; set; }
    }
}
