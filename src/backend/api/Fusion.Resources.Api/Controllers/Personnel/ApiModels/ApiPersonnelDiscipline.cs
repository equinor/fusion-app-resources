using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonnelDiscipline
    {
        public ApiPersonnelDiscipline(string discipline)
        {
            Name = discipline;
        }
        public ApiPersonnelDiscipline(QueryPersonnelDiscipline discipline)
        {
            Name = discipline.Name;
        }

        public string Name { get; set; }
    }


}
