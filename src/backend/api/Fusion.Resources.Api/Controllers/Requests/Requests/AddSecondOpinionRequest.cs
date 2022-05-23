using FluentValidation;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class AddSecondOpinionRequest
    {
        public string Description { get; set; } = null!;
        public List<PersonReference> AssignedTo { get; set; } = null!;

        public class Validator : AbstractValidator<AddSecondOpinionRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.AssignedTo).NotEmpty();
            }
        }
    }
}
