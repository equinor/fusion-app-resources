using Fusion.Integration.Profile;
using Fusion.Services.LineOrg.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class QueryDepartment
    {
        public QueryDepartment(ApiOrgUnitBase lineOrgUnit)
        {
            FullDepartment = lineOrgUnit.FullDepartment;
            Identifier = lineOrgUnit.SapId;
            Name = lineOrgUnit.Name;
            ShortName = lineOrgUnit.ShortName;

            if (lineOrgUnit.Management is not null)
            {
                var profile = lineOrgUnit.Management.Persons.FirstOrDefault();
                if (profile is not null) {
                    LineOrgResponsible = new FusionPersonProfile(Enum.Parse<FusionAccountType>(profile.AccountType), profile.Upn, profile.AzureUniqueId, profile.Name)
                    {
                        Mail = profile.Mail,
                        Department = profile.Department,
                        FullDepartment = profile.FullDepartment,
                        JobTitle = profile.JobTitle,
                        MobilePhone = profile.MobilePhone,
                        OfficeLocation = profile.OfficeLocation,
                        ManagerAzureUniqueId = profile.ManagerAzureUniqueId,
                        AccountClassification = !string.IsNullOrEmpty(profile.AccountClassification) ? Enum.Parse<AccountClassification>(profile.AccountClassification) : null,
                        IsExpired = profile.IsExpired,
                        ExpiredDate = profile.ExpiredDate,
                    };
                }

            }

        }

        /// <summary>
        /// Identifier for the org unit in it's master system, SAP or workday. String should support both.
        /// </summary>
        public string? Identifier { get; set; }
        /// <summary>
        /// The full name of the department
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// The 3-4 char name of the department, used as one part in the full department string.
        /// </summary>
        public string? ShortName { get; set; }

        public string FullDepartment { get; }
        public string? SectorId { get; set; }

        public FusionPersonProfile? LineOrgResponsible { get; set; }
        public List<FusionPersonProfile>? DelegatedResourceOwners { get; set; }
        public bool IsTracked { get; set; } = false;
    }
}
