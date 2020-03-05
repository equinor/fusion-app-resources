using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(BinderType = typeof(ProjectResolver))]
    public class ProjectIdentifier
    {
        public ProjectIdentifier(string originalIdentifier, Guid projectId, string name)
        {
            OriginalIdentifier = originalIdentifier;
            ProjectId = projectId;
            Name = name;
        }

        [JsonIgnore]
        public string OriginalIdentifier { get; set; }

        [JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        public Guid? ContextId { get; set; }
        [JsonIgnore]
        public Guid ProjectId { get; set; }

        [JsonIgnore]
        public Guid? LocalEntityId { get; set; }

    }


}
