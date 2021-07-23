using System;
using FluentValidation;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateResponsibilityMatrixRequest
    {
        public Guid? ProjectId { get; set; }
        public Guid? LocationId { get; set; }
        public string? Discipline { get; set; }
        public Guid? BasePositionId { get; set; } 
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public Guid? ResponsibleId { get; set; }



        public void LoadCommand(CreateResponsibilityMatrix command)
        {
            command.ProjectId = ProjectId;
            command.LocationId = LocationId;
            command.Discipline = Discipline;
            command.BasePositionId = BasePositionId;
            command.Sector = Sector;
            command.Unit = Unit;
            command.ResponsibleId = ResponsibleId;
        }

        public void LoadCommand(UpdateResponsibilityMatrix command)
        {
            command.ProjectId = ProjectId;
            command.LocationId = LocationId;
            command.Discipline = Discipline;
            command.BasePositionId = BasePositionId;
            command.Sector = Sector;
            command.Unit = Unit;
            command.ResponsibleId = ResponsibleId;
        }

        #region Validation

        public class Validator : AbstractValidator<UpdateResponsibilityMatrixRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Discipline).NotContainScriptTag();
                RuleFor(x => x.Discipline).MaximumLength(50);
                RuleFor(x => x.Sector).NotContainScriptTag();
                RuleFor(x => x.Sector).MaximumLength(100);
                RuleFor(x => x.Unit).NotContainScriptTag();
                RuleFor(x => x.Unit).MaximumLength(100);
            }
        }

        #endregion
    }
}
