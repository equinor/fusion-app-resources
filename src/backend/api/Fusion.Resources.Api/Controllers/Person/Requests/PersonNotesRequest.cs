using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class PersonNotesRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool IsShared { get; set; }

        public class Validator : AbstractValidator<PersonNotesRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Title).NotContainScriptTag()
                    .MaximumLength(250);
                RuleFor(x => x.Content).NotContainScriptTag()
                    .MaximumLength(2500);
            }
        }
    }


}