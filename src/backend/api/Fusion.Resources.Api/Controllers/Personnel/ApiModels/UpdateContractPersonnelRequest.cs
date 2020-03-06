using Fusion.Resources.Domain.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateContractPersonnelRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public string JobTitle { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public List<PersonnelDisciplineEntity>? Disciplines { get; set; }

        public void LoadCommand(UpdateContractPersonnel command)
        {
            command.FirstName = FirstName;
            command.LastName = LastName;
            command.JobTitle = JobTitle;
            command.Phone = PhoneNumber;
            command.Disciplines = Disciplines?.Select(d => d.Name).ToList() ?? new List<string>();
        }
    }
}
