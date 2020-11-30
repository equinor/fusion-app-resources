using Bogus;
using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Test
{
    public static class ApiContractBuilder
    {
        private static readonly Faker faker = new Faker();

        public static ApiProjectContractV2 NewContract(Guid contractId)
        {
            var contract = new ApiProjectContractV2()
            {
                Id = contractId,
                ContractNumber = faker.Finance.Account(10),
                Name = faker.Hacker.Phrase()
            };

            return contract;
        }

        public static ApiProjectContractV2 WithCompanyRep(this ApiProjectContractV2 contract, Guid userId)
        {
            contract.ContractRep = new ApiPositionV2()
            {
                Id = Guid.NewGuid(),
                Instances = new List<ApiPositionInstanceV2>
                {
                    new ApiPositionInstanceV2
                    {
                        AppliesFrom = faker.Date.Past(),
                        AppliesTo = faker.Date.Future(),
                        AssignedPerson = new ApiPersonV2()
                        {
                            AzureUniqueId = userId
                        }
                    }
                }
            };
            return contract;
        }

        public static ApiProjectContractV2 WithExternalCompanyRep(this ApiProjectContractV2 contract, Guid userId)
        {
            contract.ExternalCompanyRep = new ApiPositionV2()
            {
                Id = Guid.NewGuid(),
                Instances = new List<ApiPositionInstanceV2>
                {
                    new ApiPositionInstanceV2
                    {
                        AppliesFrom = faker.Date.Past(),
                        AppliesTo = faker.Date.Future(),
                        AssignedPerson = new ApiPersonV2()
                        {
                            AzureUniqueId = userId
                        }
                    }
                }
            };
            return contract;
        }

        public static ApiProjectContractV2 WithExternalContractRep(this ApiProjectContractV2 contract, Guid userId)
        {
            contract.ExternalContractRep = new ApiPositionV2()
            {
                Id = Guid.NewGuid(),
                Instances = new List<ApiPositionInstanceV2>
                {
                    new ApiPositionInstanceV2
                    {
                        AppliesFrom = faker.Date.Past(),
                        AppliesTo = faker.Date.Future(),
                        AssignedPerson = new ApiPersonV2()
                        {
                            AzureUniqueId = userId
                        }
                    }
                }
            };
            return contract;
        }
    }
}
