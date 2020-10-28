using Bogus;
using Fusion.ApiClients.Org;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fusion.Testing.Mocks.OrgService
{
    public class FusionTestContractBuilder
    {
        private readonly ApiProjectV2 project;
        private readonly ApiProjectContractV2 contract;
        private readonly List<ApiPositionV2> positions = new List<ApiPositionV2>();
        private readonly Faker faker = new Faker();

        public FusionTestContractBuilder()
        {
            project = OrgTestData.Project().Generate();
            contract = OrgTestData.Contract().Generate();
        }

        public FusionTestContractBuilder(ApiProjectV2 project)
        {
            this.project = project;
            contract = OrgTestData.Contract().Generate();
        }

        public ApiProjectContractV2 Contract => contract;
        public IEnumerable<ApiPositionV2> Positions 
        { 
            get
            {
                var serialized = JsonConvert.SerializeObject(positions);
                var deserialized = JsonConvert.DeserializeObject<List<ApiPositionV2>>(serialized);

                deserialized.ForEach(p =>
                {
                    p.ContractId = contract.Id;
                    p.Contract = new ApiContractReferenceV2
                    {
                        Company = contract.Company,
                        Id = contract.Id,
                        ContractNumber = contract.ContractNumber,
                        Name = contract.Name
                    };
                    p.ProjectId = project.ProjectId;
                    p.Project = new ApiProjectReferenceV2()
                    {
                        ProjectId = project.ProjectId,
                        DomainId = project.DomainId,
                        Name = project.Name,
                        ProjectType = project.ProjectType
                    };
                });

                return deserialized;
            } 
        }

        public FusionTestContractBuilder(ApiProjectContractV2 contract)
        {
            this.contract = contract;
        }

        public FusionTestContractBuilder WithName(string name)
        {
            contract.Name = name;
            return this;
        }

        public FusionTestContractBuilder WithNumber(string number)
        {
            contract.ContractNumber = number;
            return this;
        }

        public FusionTestContractBuilder WithCompany(Guid id, string name)
        {
            contract.Company = new ApiCompanyV2 { Id = id, Name = name };
            return this;
        }
        public FusionTestContractBuilder WithId(Guid id)
        {
            contract.Id = id;
            return this;
        }

        public FusionTestContractBuilder WithPositions(int count) => WithPositions(count, count);
        public FusionTestContractBuilder WithPositions(int min = 3, int max = 10)
        {
            var count = faker.Random.Int(min, max);
            var newPositions = OrgTestData.Position().Generate(count);
            positions.AddRange(newPositions);

            return this;
        }

        public FusionTestContractBuilder WithCompanyRep(ApiPositionV2 position = null)
        {
            contract.CompanyRep = position ?? OrgTestData.Position();
            return this;
        }
        public FusionTestContractBuilder WithContractRep(ApiPositionV2 position = null)
        {
            contract.ContractRep = position ?? OrgTestData.Position();
            return this;
        }
        public FusionTestContractBuilder WithExternalCompanyRep(ApiPositionV2 position = null)
        {
            contract.ExternalCompanyRep = position ?? OrgTestData.Position();
            positions.Add(position);            

            return this;
        }

        public FusionTestContractBuilder WithExternalContractRep(ApiPositionV2 position = null)
        {
            contract.ExternalContractRep= position ?? OrgTestData.Position();
            positions.Add(position);
            return this;
        }
    }
}
