using Fusion.ApiClients.Org;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiCompany
    {
        public ApiCompany(ApiCompanyV2 company)
        {
            Id = company.Id;
            Name = company.Name;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
