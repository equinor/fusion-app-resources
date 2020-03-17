using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiBasePosition
    {
        public ApiBasePosition(QueryBasePosition basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Discipline = basePosition.Disicipline;
            ProjectType = basePosition.ProjectType;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Discipline { get; set; }
        public string ProjectType { get; set; }
    }



}
