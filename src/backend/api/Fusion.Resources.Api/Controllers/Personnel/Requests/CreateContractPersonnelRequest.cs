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
        public string PhoneNumber { get; set; } = null!;

        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }

        public List<PersonnelDisciplineEntity>? Disciplines { get; set; }

        public void LoadCommand(CreateContractPersonnel command)
        {
            command.FirstName = FirstName;
            command.LastName = LastName;
            command.Phone = PhoneNumber;
            command.JobTitle = JobTitle;
            command.DawinciCode = DawinciCode;
            command.LinkedInProfile = LinkedInProfile;

            command.Disciplines = Disciplines?.Select(d => d.Name).ToList() ?? new List<string>();
        }


        #region Validation

        public class Validator : AbstractValidator<CreateContractPersonnelRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Mail).IsValidEmail().MaximumLength(100).WithMessage("Mail must be valid").WithName("mail");
                
                RuleFor(x => x.FirstName).NotEmpty().WithName("firstName");
                RuleFor(x => x.FirstName).NotContainScriptTag();
                RuleFor(x => x.FirstName).MaximumLength(50).WithName("firstName");


                RuleFor(x => x.LastName).NotContainScriptTag();
                RuleFor(x => x.LastName).MaximumLength(50).WithName("lastName");
                
                
                RuleFor(x => x.JobTitle).MaximumLength(100).WithName("jobTitle").When(x => x.JobTitle != null);
                RuleFor(x => x.JobTitle).NotContainScriptTag();


                RuleFor(x => x.PhoneNumber).MaximumLength(50).WithName("phoneNumber").When(x => x.PhoneNumber != null);
                RuleFor(x => x.PhoneNumber).NotContainScriptTag();


                RuleFor(x => x.DawinciCode).MaximumLength(50).WithName("dawinciCode").When(x => x.DawinciCode != null);
                RuleFor(x => x.DawinciCode).NotContainScriptTag();

                RuleFor(x => x.LinkedInProfile).MaximumLength(100).WithName("linkedInProfile").When(x => x.LinkedInProfile != null);
                RuleFor(x => x.LinkedInProfile)
                    .Must(x => x!.StartsWith("https://www.linkedin.com") || x!.StartsWith("http://www.linkedin.com"))
                    .WithName("linkedInProfile")
                    .WithMessage("Linked in profile must be a valid url to the linked in profile in the form of 'https://www.linkedin.com/...'")
                    .When(x => x.LinkedInProfile != null);
                RuleFor(x => x.LinkedInProfile).NotContainScriptTag();


                RuleForEach(x => x.Disciplines).Must(d => d.Name != null).WithMessage("Discipline name must be specified. Empty value is not allowed.").WithName("disciplines.name")
                    .When(x => x.Disciplines != null);
                RuleForEach(x => x.Disciplines).Must(d => d.Name?.Length < 150).WithMessage("Discipline name cannot exceed 150 characters.").WithName("disciplines.name")
                    .When(x => x.Disciplines != null);
            }

        }

        #endregion

    }
}
