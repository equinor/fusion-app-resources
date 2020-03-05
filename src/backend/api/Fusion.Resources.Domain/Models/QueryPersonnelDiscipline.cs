using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelDiscipline
    {
        public QueryPersonnelDiscipline(DbPersonnelDiscipline discipline)
        {
            Name = discipline.Name;
        }

        public string Name { get; set; }
    }
}
