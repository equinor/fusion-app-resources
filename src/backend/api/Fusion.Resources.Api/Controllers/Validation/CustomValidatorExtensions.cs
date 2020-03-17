using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public static class CustomValidatorExtensions
    {

        //public static IRuleBuilderOptions<T, IList<InstanceRequest>> HaveValidInstances<T>(this IRuleBuilder<T, IList<InstanceRequest>> ruleBuilder)
        //{
        //    return ruleBuilder.SetValidator(new CustomValidator<IList<InstanceRequest>>((list, context) =>
        //    {
        //        // Check for duplicate ids
        //        if (list.Where(i => i.Id.HasValue && i.Id != Guid.Empty).GroupBy(i => i.Id).Any(g => g.Count() > 1))
        //            context.AddFailure(new ValidationFailure(context.JsPropertyName(), "Detected duplicate instance ids. These must be unique when updating. To create new, use null."));

        //        int index = 0;

        //        foreach (var instance in list)
        //        {
        //            string basePropertyPath = $"{context.JsPropertyName()}[{index}]";

        //            if (instance.AssignedPerson != null)
        //            {
        //                if (instance.AssignedPerson.AzureUniqueId.HasValue && instance.AssignedPerson.AzureUniqueId == Guid.Empty)
        //                    context.AddFailure(new ValidationFailure($"{basePropertyPath}.assignedPerson.azureUniqueId", "Person unique object id cannot be empty-guid when provided."));

        //                if (!string.IsNullOrEmpty(instance.AssignedPerson.Mail) && !IsValidEmail(instance.AssignedPerson.Mail))
        //                    context.AddFailure(new ValidationFailure($"{basePropertyPath}.assignedPerson.mail", "Invalid mail address", instance.AssignedPerson.Mail));

        //                if (instance.AssignedPerson.AzureUniqueId is null && string.IsNullOrEmpty(instance.AssignedPerson.Mail))
        //                    context.AddFailure(new ValidationFailure($"{basePropertyPath}.assignedPerson", "Either azureUniqueId or mail must be specified"));
        //            }

        //            if (instance.ParentPositionId.HasValue && instance.ParentPositionId == Guid.Empty)
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.parentPositionId", "Position id must not be 0-guid"));

        //            if (instance.TaskOwnerIds != null && instance.TaskOwnerIds.Any(id => id == Guid.Empty))
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.taskOwnerIds", "Position id must not be 0-guid"));

        //            if (HasScriptTags(instance.ExternalId))
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.externalId", "Text field cannot contain script tags"));

        //            if (HasScriptTags(instance.Obs))
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.obs", "Text field cannot contain script tags"));

        //            if (instance.Location != null && instance.Location.Id == Guid.Empty)
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.location", "Location id cannot be empty-guid when location is provided."));

        //            if (instance.AppliesFrom == null)
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.appliesFrom", "From date must be set."));

        //            if (instance.AppliesTo == null)
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.appliesTo", "To date must be set."));

        //            if (instance.AppliesFrom.HasValue && instance.AppliesTo.HasValue)
        //                if (instance.AppliesTo < instance.AppliesFrom)
        //                    context.AddFailure(new ValidationFailure($"{basePropertyPath}.appliesTo", "To date cannot be earlier than from date", $"{instance.AppliesFrom} -> {instance.AppliesTo}"));

        //            if (instance.Workload == null)
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.workload", "Workload must be set."));

        //            if (instance.Workload.HasValue && instance.Workload < 0)
        //            {
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.workload", "Workload cannot be less than 0.", instance.Workload));
        //            }

        //            if (HasScriptTags(instance.Calendar))
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.rotation.calendar", "Text field cannot contain script tags."));

        //            if (HasScriptTags(instance.RotationId))
        //                context.AddFailure(new ValidationFailure($"{basePropertyPath}.rotation.rotationId", "Text field cannot contain script tags."));

        //            index++;
        //        }

        //    }));
        //}

        //public static IRuleBuilderOptions<T, BasePositionRef> HaveValidBasePosition<T>(this IRuleBuilder<T, BasePositionRef> ruleBuilder)
        //{
        //    return ruleBuilder.SetValidator(new CustomValidator<BasePositionRef>((basePosition, context) =>
        //    {
        //        if (basePosition == null)
        //            context.AddFailure(new ValidationFailure(context.JsPropertyName(), "Base position must be specified"));

        //        if (basePosition != null)
        //        {
        //            if (basePosition.Id == Guid.Empty)
        //                context.AddFailure(new ValidationFailure(context.JsPropertyName(), "Base position id cannot be empty-guid", basePosition.Id));
        //        }
        //    }));
        //}

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
