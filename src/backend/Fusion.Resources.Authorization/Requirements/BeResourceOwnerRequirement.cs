﻿using Fusion.Authorization;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Authorization.Requirements
{
    /// <summary>
    /// Adjustment of the same type provided by the intergration lib. 
    /// This will work against a claim added by the local transformer. This will resolve the role provided by the line org for manager responsebility based on SAP data. 
    /// This update is harder to update in the integration lib claims transformer, due to optimalization.
    /// </summary>
    public class BeResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
    {
        public BeResourceOwnerRequirement(string departmentPath, bool includeParents = false, bool includeDescendants = false)
        {
            DepartmentPath = departmentPath;
            IncludeParents = includeParents;
            IncludeDescendants = includeDescendants;
        }

        public BeResourceOwnerRequirement()
        {
        }


        public override string Description => ToString();

        public override string Code => "ResourceOwner";

        public string? DepartmentPath { get; }
        public bool IncludeParents { get; }
        public bool IncludeDescendants { get; }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            var departments = context.User.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment)
                .Select(c => c.Value);

            if (!departments.Any())
            {
                SetEvaluation("User is not resource owner in any departments");
                return Task.CompletedTask;
            }
            if (string.IsNullOrEmpty(DepartmentPath))
            {
                context.Succeed(this);
                return Task.CompletedTask;
            }

            // responsibility descendant Descendants
            var directResponsibility = departments.Any(d => d.Equals(DepartmentPath, StringComparison.OrdinalIgnoreCase));
            var descendantResponsibility = departments.Any(d => d.StartsWith(DepartmentPath, StringComparison.OrdinalIgnoreCase));
            var parentResponsibility = departments.Any(d => DepartmentPath.StartsWith(d, StringComparison.OrdinalIgnoreCase));

            var hasAccess = directResponsibility
                || IncludeParents && parentResponsibility
                || IncludeDescendants && descendantResponsibility;

            if (hasAccess)
            {
                SetEvaluation($"User has access though responsibility in {string.Join(", ", departments)}. " +
                    $"[owner in department={directResponsibility}, parents={parentResponsibility}, descendants={descendantResponsibility}]");

                context.Succeed(this);
            }

            SetEvaluation($"User have responsibility in departments: {string.Join(", ", departments)}; But not in the requirement '{DepartmentPath}'");

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(DepartmentPath))
                return "User must be resource owner of a department";

            if (IncludeParents && IncludeDescendants)
                return $"User must be resource owner in department '{DepartmentPath}' or any departments above or below";

            if (IncludeParents)
                return $"User must be resource owner in department '{DepartmentPath}' or any departments above";

            if (IncludeDescendants)
                return $"User must be resource owner in department '{DepartmentPath}' or any sub departments";

            return $"User must be resource owner in department '{DepartmentPath}'";
        }
    }
}