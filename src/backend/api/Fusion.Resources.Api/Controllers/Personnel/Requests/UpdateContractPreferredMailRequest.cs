using FluentValidation;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateContractPreferredMailRequest
    {
        public List<PersonnelPreferredContact> Personnel { get; set; } = new List<PersonnelPreferredContact>();

        public class PersonnelPreferredContact
        {
            public Guid PersonnelId { get; set; }
            public string? PreferredContactMail { get; set; } 
        }

        public class Validator : AbstractValidator<UpdateContractPreferredMailRequest>
        {
            public Validator()
            {
                RuleForEach(p => p.Personnel).ChildRules(c =>
                {
                    c.RuleFor(x => x.PreferredContactMail).NotHaveInvalidMailDomain();
                    c.RuleFor(x => x.PreferredContactMail).IsValidEmail()
                        .When(x => x.PreferredContactMail != null);
                }).When(x => x.Personnel != null);
            }
        }
    }
}
