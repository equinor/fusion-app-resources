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
