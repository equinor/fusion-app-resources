﻿using FluentValidation;
using Fusion.Resources.Domain.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateContractPersonnelRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public string? JobTitle { get; set; }
        public string PhoneNumber { get; set; } = null!;

        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }

        public string? PreferredContactMail { get; set; }

        public List<PersonnelDisciplineEntity>? Disciplines { get; set; }

        public void LoadCommand(UpdateContractPersonnel command)
        {
            command.FirstName = FirstName;
            command.LastName = LastName;
            command.JobTitle = JobTitle;
            command.DawinciCode = DawinciCode;
            command.LinkedInProfile = LinkedInProfile;
            command.Phone = PhoneNumber;
            command.Disciplines = Disciplines?.Select(d => d.Name).ToList() ?? new List<string>();
            command.PreferredContactMail = PreferredContactMail;
        }

        #region Validation

        public class Validator : AbstractValidator<UpdateContractPersonnelRequest>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty().WithName("firstName");
                RuleFor(x => x.FirstName).NotContainScriptTag();
                RuleFor(x => x.FirstName).MaximumLength(50).WithName("firstName");


                RuleFor(x => x.LastName).NotContainScriptTag();
                RuleFor(x => x.LastName).MaximumLength(50).WithName("lastName");


                RuleFor(x => x.JobTitle).MaximumLength(100).WithName("jobTitle").When(x => x.JobTitle != null);
                RuleFor(x => x.JobTitle).NotContainScriptTag();


                RuleFor(x => x.PhoneNumber).MaximumLength(30).WithName("phoneNumber").When(x => x.PhoneNumber != null);
                RuleFor(x => x.PhoneNumber).NotContainScriptTag();


                RuleFor(x => x.DawinciCode).MaximumLength(50).WithName("dawinciCode").When(x => x.DawinciCode != null);
                RuleFor(x => x.DawinciCode).NotContainScriptTag();

                RuleFor(x => x.LinkedInProfile).MaximumLength(100).WithName("linkedInProfile").When(x => x.LinkedInProfile!= null);
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

                RuleFor(x => x.PreferredContactMail)
                    .IsValidEmail()
                        .When(m => m.PreferredContactMail != null)
                    .NotHaveInvalidMailDomain()
                        .When(m => m.PreferredContactMail != null);
            }

        }

        #endregion
    }
}
