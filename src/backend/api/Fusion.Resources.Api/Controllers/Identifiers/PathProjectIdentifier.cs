﻿using System;
using System.Text.Json.Serialization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(BinderType = typeof(ProjectResolver))]
    public class PathProjectIdentifier
    {
        public PathProjectIdentifier(string originalIdentifier, Guid projectId, string name)
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

        public static implicit operator ProjectIdentifier (PathProjectIdentifier apiModel)
        {
            return new ProjectIdentifier(apiModel.ProjectId, apiModel.Name)
            {
                LocalEntityId = apiModel.LocalEntityId,
            };
        }
    }
}
