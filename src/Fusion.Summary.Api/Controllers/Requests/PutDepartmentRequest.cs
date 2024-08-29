using FluentValidation;

namespace Fusion.Summary.Api.Controllers.Requests;

public record PutDepartmentRequest(string FullDepartmentName, Guid[] ResourceOwnersAzureUniqueId, Guid[] DelegateResourceOwnersAzureUniqueId)
{
    public class Validator : AbstractValidator<PutDepartmentRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FullDepartmentName).NotEmpty();
            RuleFor(x => x.ResourceOwnersAzureUniqueId.Concat(x.DelegateResourceOwnersAzureUniqueId))
                .NotEmpty()
                .WithMessage($"Either {nameof(ResourceOwnersAzureUniqueId)} or {nameof(DelegateResourceOwnersAzureUniqueId)} must contain at least one element.");
        }
    }
};

