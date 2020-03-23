using FluentValidation;
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

        public string? DawinciCode { get; set; }

        public List<PersonnelDisciplineEntity>? Disciplines { get; set; }

        public void LoadCommand(CreateContractPersonnel command)
        {
            command.FirstName = FirstName;
            command.LastName = LastName;
            command.Phone = PhoneNumber ?? string.Empty;
            command.JobTitle = JobTitle;
            command.DawinciCode = DawinciCode;
            command.Disciplines = Disciplines?.Select(d => d.Name).ToList() ?? new List<string>();
        }


        #region Validation

        public class Validator : AbstractValidator<CreateContractPersonnelRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Mail).IsValidEmail().WithMessage("Mail must be valid").WithName("mail");
                
                RuleFor(x => x.FirstName).NotEmpty().WithName("firstName");
                RuleFor(x => x.FirstName).NotContainScriptTag();
                RuleFor(x => x.FirstName).MaximumLength(100).WithName("firstName");


                RuleFor(x => x.LastName).NotContainScriptTag();
                RuleFor(x => x.LastName).MaximumLength(100).WithName("lastName");
                
                
                RuleFor(x => x.JobTitle).MaximumLength(100).WithName("jobTitle").When(x => x.JobTitle != null);
                RuleFor(x => x.JobTitle).NotContainScriptTag();


                RuleFor(x => x.PhoneNumber).MaximumLength(50).WithName("phoneNumber").When(x => x.PhoneNumber != null);
                RuleFor(x => x.PhoneNumber).NotContainScriptTag();


                RuleForEach(x => x.Disciplines).Must(d => d.Name != null).WithMessage("Discipline name must be specified. Empty value is not allowed.").WithName("disciplines.name")
                    .When(x => x.Disciplines != null);
                RuleForEach(x => x.Disciplines).Must(d => d.Name?.Length < 150).WithMessage("Discipline name cannot exceed 150 characters.").WithName("disciplines.name")
                    .When(x => x.Disciplines != null);
            }

        }

        #endregion

    }
}
