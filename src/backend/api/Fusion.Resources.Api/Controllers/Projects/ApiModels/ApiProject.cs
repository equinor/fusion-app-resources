using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers.Projects
{
    public class ApiProject
    {
        public ApiProject(QueryProject p)
        {
            Id = p.Id;
            Name = p.Name;
            OrgProjectId = p.OrgProjectId;
        }

        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public Guid OrgProjectId { get; set; }
    }
}
