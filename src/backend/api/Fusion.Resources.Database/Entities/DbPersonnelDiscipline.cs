using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbPersonnelDiscipline
    {
        public Guid Id { get; set; }
        public Guid PersonnelId { get; set; }
        public string Name { get; set; }
    }

}
