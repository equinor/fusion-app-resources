using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiBasePosition
    {
        public ApiBasePosition(Guid id, string name, string discipline, string projectType)
        {
            Id = id;
            Name = name;
            Discipline = discipline;
            ProjectType = projectType;
        }

        public ApiBasePosition(QueryBasePosition basePosition)
        {
            Id = basePosition.Id;
            Name = basePosition.Name;
            Discipline = basePosition.Discipline;
            ProjectType = basePosition.ProjectType;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Discipline { get; set; }
        public string ProjectType { get; set; }
    }



}
