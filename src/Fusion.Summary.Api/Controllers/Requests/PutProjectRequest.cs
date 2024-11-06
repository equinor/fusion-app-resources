using FluentValidation;

namespace Fusion.Summary.Api.Controllers.Requests;

public class PutProjectRequest
{
    public required string Name { get; set; }
    public required Guid OrgProjectExternalId { get; set; }

    public Guid? DirectorAzureUniqueId { get; set; }

    public Guid[] AssignedAdminsAzureUniqueId { get; set; } = [];


    public class Validator : AbstractValidator<PutProjectRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.OrgProjectExternalId).NotEmpty();
        }
    }
}