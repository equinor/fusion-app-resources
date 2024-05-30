using Fusion.Integration.Profile;
using Fusion.Services.LineOrg.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class QueryDepartment
    {
        public QueryDepartment(ApiOrgUnit lineOrgUnit)
        {
            DepartmentId = lineOrgUnit.FullDepartment;

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

        public QueryDepartment(ApiDepartment lineOrgDepartment, FusionPersonProfile? manager)
        {
            DepartmentId = lineOrgDepartment.FullName;
            LineOrgResponsible = manager;
        }

        public QueryDepartment(string departmentId, string? sectorId)
        {
            DepartmentId = departmentId;
            SectorId = sectorId;
        }

        public string DepartmentId { get; }
        public string? SectorId { get; set; }

        public FusionPersonProfile? LineOrgResponsible { get; set; }
        public List<FusionPersonProfile>? DelegatedResourceOwners { get; set; }
        public bool IsTracked { get; set; } = false;
    }
}
