using FluentValidation;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractRequest
    {
        public Guid? Id { get; set; }
        public string ContractNumber { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public CompanyReference? Company { get; set; }

        public Guid? ContractResponsiblePositionId { get; set; }
        public Guid? CompanyRepPositionId { get; set; }
        public Guid? ExternalContractResponsiblePositionId { get; set; }
        public Guid? ExternalCompanyRepPositionId { get; set; }


        #region Validator

        public class Validator : AbstractValidator<ContractRequest>
        {
            public Validator(ICompanyResolver companyResolver)
            {
                RuleFor(x => x.Id).NotEmptyIfProvided().WithName("id");
                RuleFor(x => x.ContractNumber).MaximumLength(15).WithMessage("Contractnumber should not exceed 10 characters according to SAP rules.");
                RuleFor(x => x.Description).NotContainScriptTag();
                RuleFor(x => x.Description).MaximumLength(5000);

                RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
                    .WithMessage("Start date cannot be after end date")
                    .When(x => x.StartDate.HasValue && x.EndDate.HasValue);


                RuleFor(x => x.Company)
                    .MustAsync(async (c, cancel) =>
                    {
                        var resolvedCompany = await companyResolver.FindCompanyAsync(c.Id);
                        return resolvedCompany != null;
                    })
                    .WithMessage(x => $"Could not resolve company with id '{x.Company?.Id}'")
                    .When(x => x.Company != null);

                // TODO: Add validator for position, but must implement "search" for position endpoint.
            }
        }

        #endregion
    }
}
