using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Logic.Commands
{
    public static class CustomValidatorExtensions
    {
        public static IRuleBuilderOptions<T, Dictionary<string, object>?> BeValidProposedChanges<T>(this IRuleBuilder<T, Dictionary<string, object>?> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new CustomValidator<Dictionary<string, object>>(
                (prop, context) =>
                {
                    foreach (var k in prop.Keys.Where(k => k.Length > 100))
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.key",
                            "Key cannot exceed 100 characters", k));
                    }

                }));
        }
        public static IRuleBuilderOptions<T, Domain.ResourceAllocationRequest.QueryPositionInstance?> BeValidPositionInstance<T>(this IRuleBuilder<T, Domain.ResourceAllocationRequest.QueryPositionInstance?> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new CustomValidator<Domain.ResourceAllocationRequest.QueryPositionInstance>(
                (position, context) =>
                {
                    if (position == null) return;

                    if (position.AppliesTo < position.AppliesFrom)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.appliesTo",
                            $"To date cannot be earlier than from date, {position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}",
                            $"{position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}"));


                    if (position.Obs?.Length > 30)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.obs",
                            "Obs cannot exceed 30 characters", position.Obs));

                    if (position.Workload < 0)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload",
                            "Workload cannot be less than 0", position.Workload));

                    if (position.Workload > 100)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload",
                            "Workload cannot be more than 100", position.Workload));
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
