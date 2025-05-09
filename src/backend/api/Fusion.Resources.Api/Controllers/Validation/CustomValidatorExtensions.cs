using AdaptiveExpressions;
using FluentValidation;
using FluentValidation.Results;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Org;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public static class CustomValidatorExtensions
    {

        /// <summary>
        /// Checks if the org unit exists in line org. 
        /// The provided value can be either a full department string or the SAP id.
        /// </summary>
        public static IRuleBuilderOptions<T, string?> BeValidOrgUnit<T>(this IRuleBuilder<T, string?> ruleBuilder, IServiceProvider services)
        {
            var result = ruleBuilder.CustomAsync(async (value, context, ct) =>
            {
                if (value!= null)
                {
                    var lineorg = services.GetRequiredService<ILineOrgResolver>();


                    var departmentId = value.IsNumber() ? DepartmentId.FromSapId(value) : DepartmentId.FromFullPath(value);

                    var orgUnit = await lineorg.ResolveOrgUnitAsync(departmentId);

                    if (orgUnit == null)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}", $"Org unit does not seem to exist. Used {value} as reference value."));
                    }
                }
            });

            return (IRuleBuilderOptions<T, string?>)result;
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

        public static IRuleBuilderOptions<T, Guid?> BeValidChangeRequestPosition<T>(this IRuleBuilder<T, Guid?> ruleBuilder, IProjectOrgResolver projectOrgResolver)
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

                    if (position != null && position.Instances.Count > 1)
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}",
                            $"Position with id '{positionId}' exists, but have multiple instances, {position.Instances.Count}, which is not supported."));
                    }
                }
            });

            return (IRuleBuilderOptions<T, Guid?>)result;
        }

        public static IRuleBuilderOptionsConditions<T, Dictionary<string, object>?> BeValidProposedChanges<T>(this IRuleBuilder<T, Dictionary<string, object>?> ruleBuilder)
        {
            return ruleBuilder.Custom((proposedChanges, context) =>
            {
                if (proposedChanges is null)
                {
                    context.AddFailure("Value not provided");
                    return;
                }

                ValidateSerializedSize(proposedChanges, context);
                ValidateKeyLengths(proposedChanges, context);
                ValidateAllowedKeysAndValues(proposedChanges, context);
            });
        }

        #region BeValidProposedChangesHelpers

        private static void ValidateSerializedSize<T>(Dictionary<string, object> prop, ValidationContext<T> context)
        {
            var serialized = JsonConvert.SerializeObject(prop);
            if (serialized.Length > 5000)
            {
                context.AddFailure("Total size of change dictionary cannot be larger than 5000 characters");
            }
        }

        private static void ValidateKeyLengths<T>(Dictionary<string, object> prop, ValidationContext<T> context)
        {
            foreach (var key in prop.Keys.Where(k => k.Length > 100))
            {
                context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.key",
                    "Key cannot exceed 100 characters", key));
            }
        }

        private static void ValidateAllowedKeysAndValues<T>(Dictionary<string, object> prop, ValidationContext<T> context)
        {
            string[] allowedProperties = ["appliesFrom", "appliesTo", "location", "workload", "basePosition"];
            var isInvalid = false;

            foreach (var key in prop.Keys)
            {
                if (allowedProperties.Any(p => string.Equals(p, key, StringComparison.OrdinalIgnoreCase)))
                {
                    ValidateKeySpecificRules(key, prop[key], context);
                }
                else
                {
                    context.AddFailure($"Key '{key}' is not valid");
                    isInvalid = true;
                }
            }

            if (isInvalid)
            {
                context.AddFailure($"Allowed keys are {string.Join(", ", allowedProperties.Select(p => p.ToLowerFirstChar()))}");
            }
        }

        private static void ValidateKeySpecificRules<T>(string key, object? value, ValidationContext<T> context)
        {
            if (key.Equals("appliesFrom", StringComparison.OrdinalIgnoreCase) || key.Equals("appliesTo", StringComparison.OrdinalIgnoreCase))
            {
                if (!DateTime.TryParse(value?.ToString(), out var date))
                {
                    context.AddFailure($"Key '{key}' value must be a valid date");
                }
                else if (date.TimeOfDay != TimeSpan.Zero)
                {
                    context.AddFailure($"Key '{key}' value must be a date without time or time set to 00:00:00");
                }
            }
        }

        #endregion

        public static IRuleBuilderOptionsConditions<T, ApiPositionInstance?> BeValidPositionInstance<T>(this IRuleBuilder<T, ApiPositionInstance?> ruleBuilder)
        {
            return ruleBuilder.Custom((position, context) =>
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
                });
        }
   
        public static string ToLowerFirstChar(this string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);
            return newString;
        }

        public static string JsPropertyName<T>(this ValidationContext<T> context) => context.PropertyName.ToLowerFirstChar();

    }
}
