using FluentValidation;
using Fusion.Integration;

namespace Fusion.Summary.Api.Controllers.Requests;

public record PutDepartmentRequest(string FullDepartmentName, Guid ResourceOwnerAzureUniqueId)
{
    public class Validator : AbstractValidator<PutDepartmentRequest>
    {
        public Validator(IFusionProfileResolver profileResolver)
        {
            RuleFor(x => x.FullDepartmentName).NotEmpty();
            RuleFor(x => x.ResourceOwnerAzureUniqueId).NotEmpty();
            RuleFor(x => x.ResourceOwnerAzureUniqueId).MustAsync(async (azureId, _) =>
            {
                var profile = await profileResolver.ResolvePersonBasicProfileAsync(azureId);
                return profile != null;
            }).WithMessage("Resource owner could not be resolved");
        }
    }
};

