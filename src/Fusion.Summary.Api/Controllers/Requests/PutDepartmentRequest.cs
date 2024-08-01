using FluentValidation;

namespace Fusion.Summary.Api.Controllers.Requests;

public record PutDepartmentRequest(string FullDepartmentName, Guid ResourceOwnerAzureUniqueId)
{
    public class Validator : AbstractValidator<PutDepartmentRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FullDepartmentName).NotEmpty();
            RuleFor(x => x.ResourceOwnerAzureUniqueId).NotEmpty();
        }
    }
};

