using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryBasePosition
    {
        public QueryBasePosition(Guid basePositionId)
        {
            Id = basePositionId;
            Name = "[Not resolved]";
        }
        public QueryBasePosition(ApiBasePositionV2 basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Disicipline = basePosition.Discipline;
            ProjectType = basePosition.ProjectType;

            Resolved = true;
        }

        public QueryBasePosition(FusionBasePosition basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Disicipline = basePosition.Discipline;
            ProjectType = basePosition.Type;

            Resolved = true;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Disicipline { get; set; }
        public string ProjectType { get; set; }

        /// <summary>
        /// Indicates if the base position was resolved from the service. 
        /// 
        /// Null indicates resolving was not atempted. 
        /// false indicates it was not found in the collection.
        /// </summary>
        public bool? Resolved { get; set; }
    }
}

