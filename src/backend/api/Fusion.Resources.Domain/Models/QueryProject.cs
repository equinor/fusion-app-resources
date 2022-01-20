using Fusion.Resources.Database.Entities;
using System;

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
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? DomainId { get; set; }
        public Guid OrgProjectId { get; set; }

    }

    public class QueryProjectRef
    {
        public QueryProjectRef(QueryProject project)
            : this(project.OrgProjectId, project.Name, project.DomainId ?? "", "") { }

        public QueryProjectRef(Guid orgId, string name, string domainId, string type)
        {
            OrgProjectId = orgId;
            Name = name;
            DomainId = domainId;
            Type = type;
        }
        public string Name { get; set; }
        public string? DomainId { get; set; }
        public Guid OrgProjectId { get; set; }
        public string Type { get; set; }
    }
}
