using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public static class CustomValidatorExtensions
    {
        public static IRuleBuilderOptions<T, PersonReference> BeValidPerson<T>(this IRuleBuilder<T, PersonReference> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new CustomValidator<PersonReference>((person, context) =>
            {
                if (person != null)
                {
                    if (person.AzureUniquePersonId.HasValue && person.AzureUniquePersonId == Guid.Empty)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.azureUniqueId", "Person unique object id cannot be empty-guid when provided."));

                    if (!string.IsNullOrEmpty(person.Mail) && ! ValidationHelper.IsValidEmail(person.Mail))
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.mail", "Invalid mail address", person.Mail));

                    if (person.AzureUniquePersonId is null && string.IsNullOrEmpty(person.Mail))
                        context.AddFailure(new ValidationFailure(context.JsPropertyName(), "Either azureUniqueId or mail must be specified"));
                }
            }));
        }

        public static IRuleBuilderOptions<T, BasePositionReference> BeValidBasePosition<T>(this IRuleBuilder<T, BasePositionReference> ruleBuilder, IProjectOrgResolver projectOrgResolver)
        {
            var result = ruleBuilder.CustomAsync(async (bpref, context, ct) =>
            {
                if (bpref != null)
                {
                    var resolvedBp = await projectOrgResolver.ResolveBasePositionAsync(bpref.Id);
                    if (resolvedBp == null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Base position is not valid. Could not resolve {bpref?.Id} in org service."));
                    }
                }
            });

            return (IRuleBuilderOptions<T, BasePositionReference>)result;
        }

        public static IRuleBuilderOptions<T, Guid?> BeExistingContractPositionId<T>(this IRuleBuilder<T, Guid?> ruleBuilder, IProjectOrgResolver projectOrgResolver)
        {
            var result = ruleBuilder.CustomAsync(async (positionId, context, ct) =>
            {
                if (positionId.HasValue)
                {
                    var position = await projectOrgResolver.ResolvePositionAsync(positionId.Value);

                    if (position == null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Position with id '{positionId}' does not exist in org service."));
                    }

                    if (position != null && position.ContractId == null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Position with id '{positionId}' exists, but does not belong to a contract."));
                    }
                }
            });

            return (IRuleBuilderOptions<T, Guid?>)result;
        }

        public static IRuleBuilderOptions<T, Guid?> BeExistingCompanyPositionId<T>(this IRuleBuilder<T, Guid?> ruleBuilder, IProjectOrgResolver projectOrgResolver)
        {
            var result = ruleBuilder.CustomAsync(async (positionId, context, ct) =>
            {
                if (positionId.HasValue)
                {
                    var position = await projectOrgResolver.ResolvePositionAsync(positionId.Value);

                    if (position == null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Position with id '{positionId}' does not exist in org service."));
                    }

                    if (position != null && position.ContractId != null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Position with id '{positionId}' exisits, but belongs to contract {position.Contract?.ContractNumber}."));
                    }
                }
            });

            return (IRuleBuilderOptions<T, Guid?>)result;
        }


        private static string ToLowerFirstChar(this string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);
            return newString;
        }

        public static string JsPropertyName(this CustomContext context) => context.PropertyName.ToLowerFirstChar();

    }

}
