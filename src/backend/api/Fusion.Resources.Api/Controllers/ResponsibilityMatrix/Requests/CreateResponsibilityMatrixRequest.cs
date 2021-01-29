using System;
using FluentValidation;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateResponsibilityMatrixRequest
    {
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; } = null!;
        public ApiProjectReference Project { get; set; } = null!;
        public ApiLocation Location { get; set; }
        public string? Discipline { get; set; }
        public ApiBasePosition BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public ApiPerson Responsible { get; set; } = null!;



        public void LoadCommand(CreateResponsibilityMatrix command)
        {
            command.Discipline = Discipline;

        }



        #region Validation

        public class Validator : AbstractValidator<CreateResponsibilityMatrixRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Discipline).NotContainScriptTag();
                RuleFor(x => x.Discipline).MaximumLength(5000);


            }
        }

        #endregion
    }
}
