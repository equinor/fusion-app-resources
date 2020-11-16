using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IResourcesApiClient
    {
        Task<List<ProjectContract>> GetProjectContractsAsync();
        Task<PersonnelRequestList> GetTodaysContractRequests(ProjectContract projectContract, string state);
        Task<List<DelegatedRole>> RetrieveDelegatesForContractAsync(ProjectContract projectContract);

        #region Models


        public class ProjectContract
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string ContractNumber { get; set; }

            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; }

            public Guid? ContractResponsiblePositionId { get; set; }
            public Guid? CompanyRepPositionId { get; set; }
            public Guid? ExternalContractResponsiblePositionId { get; set; }
            public Guid? ExternalCompanyRepPositionId { get; set; }
        }

        public class DelegatedRole
        {
            public string Classification { get; set; }

            public Person Person { get; set; }
        }

        public class Person
        {
            public Guid? AzureUniquePersonId { get; set; }

            public string Mail { get; set; }
        }


        public class PersonnelRequestList
        {
            public List<PersonnelRequest> Value { get; set; }
        }

        public class PersonnelRequest
        {
            public Guid Id { get; set; }

            public string State { get; set; }

            public DateTimeOffset LastActivity { get; set; }

            public RequestPosition Position { get; set; }

            public RequestPersonnel Person { get; set; }

            public class RequestPosition
            {
                public string Name { get; set; }
                public DateTime AppliesFrom { get; set; }
                public DateTime AppliesTo { get; set; }
            }

            public class RequestPersonnel
            {
                public string Name { get; set; }
                public string Mail { get; set; }
            }
        }

        #endregion Models
    }
}