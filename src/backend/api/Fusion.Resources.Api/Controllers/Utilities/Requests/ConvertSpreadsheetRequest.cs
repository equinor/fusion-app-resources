using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers.Utilities
{
    public class ConvertSpreadsheetRequest
    {
        public IFormFile? File { get; set; }

        public class Validator : AbstractValidator<ConvertSpreadsheetRequest>
        {
            public Validator()
            {
                RuleFor(x => x.File).NotEmpty().WithMessage("File must be provided").WithName("file");
            }
        }
    }
}
