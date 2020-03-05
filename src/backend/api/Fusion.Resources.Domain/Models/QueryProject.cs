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
            OrgProjectId = project.OrgProjectId;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid OrgProjectId { get; set; }

    }
}
