using System;
using System.Security.Claims;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;

namespace Fusion.Resources
{
    public static class ClaimsPrincipalExtensions
    {
        public static QueryRequestOrigin GetRequestOrigin(this ClaimsPrincipal user)
        {
            return user.GetUserAccountType() switch
            {
                FusionAccountType.Consultant => QueryRequestOrigin.Company,
                FusionAccountType.Employee => QueryRequestOrigin.Company,
                FusionAccountType.External => QueryRequestOrigin.Contractor,
                FusionAccountType.Local => QueryRequestOrigin.Local,
                FusionAccountType.Application => QueryRequestOrigin.Application,
                _ => throw new InvalidOperationException($"Unable to resolve origin: '{ user.GetUserAccountType()}'.")
            };
        }

    }
}