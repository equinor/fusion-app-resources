using Fusion.Resources.Domain.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateContractPersonnelRequest
    {
        public string Mail { get; set; } = null!;

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public string? JobTitle { get; set; } = null!;
        public string? PhoneNumber { get; set; } = null!;

        public List<ApiPersonnelDiscipline>? Disciplines { get; set; }

        public void LoadCommand(CreateContractPersonnel command)
        {
            command.FirstName = FirstName;
            command.LastName = LastName;
            command.Phone = PhoneNumber;
            command.JobTitle = JobTitle;
            command.Disciplines = Disciplines?.Select(d => d.Name).ToList() ?? new List<string>();
        }
    }
}
