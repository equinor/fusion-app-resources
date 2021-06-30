using System;

namespace Fusion.Resources.Domain
{
    public class ProjectIdentifier
    {
        public ProjectIdentifier(Guid projectId, string name)
        {
            ProjectId = projectId;
            Name = name;
        }

        public Guid? LocalEntityId { get; set; }
        public Guid ProjectId { get;  }
        public string Name { get;  }
    }
}
