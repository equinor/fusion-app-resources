using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestBasePosition
    {
        public ApiRequestBasePosition(QueryBasePosition basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Discipline = basePosition.Discipline;
            ProjectType = basePosition.ProjectType;
         
            WasResolved = basePosition.Resolved;
        }

        public ApiRequestBasePosition(ApiPositionBasePositionV2 basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Discipline = basePosition.Discipline;
            ProjectType = basePosition.ProjectType;

            WasResolved = true;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Discipline { get; set; }
        public string ProjectType { get; set; }

        /// <summary>
        /// Indicates if the base position was resolved from the org chart. 
        /// IF the value is null, the resolving was not atempted.
        /// If false the base position might not exist in the org chart.
        /// </summary>
        public bool? WasResolved { get; set; }
    }
}
