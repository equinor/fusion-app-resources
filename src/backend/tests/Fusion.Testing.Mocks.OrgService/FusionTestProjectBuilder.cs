using Bogus;
using Fusion.ApiClients.Org;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Testing.Mocks.OrgService
{
    public class FusionTestProjectBuilder
    {

        private readonly ApiProjectV2 project;
        private readonly List<ApiPositionV2> positions = new List<ApiPositionV2>();
        private readonly List<ApiProjectContractV2> contracts = new List<ApiProjectContractV2>();
        private readonly Dictionary<Guid, List<ApiPositionV2>> contractPositions = new Dictionary<Guid, List<ApiPositionV2>>();
        private readonly Faker faker = new Faker();

        public FusionTestProjectBuilder()
        {
            project = OrgTestData.Project().Generate();
            positions.Add(project.Director);
        }

        public ApiProjectV2 Project => project;
        public IEnumerable<ApiPositionV2> Positions
        {
            get
            {
                var serialized = JsonConvert.SerializeObject(positions);
                var deserialized = JsonConvert.DeserializeObject<List<ApiPositionV2>>(serialized);

                deserialized.ForEach(p =>
                {
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

        public ApiPositionV2 Director => project.Director;

        public IEnumerable<ValueTuple<ApiProjectContractV2, List<ApiPositionV2>>> ContractsWithPositions =>
            contractPositions.Select(kv => (contracts.First(c => c.Id == kv.Key), kv.Value));

        public FusionTestProjectBuilder WithProjectId(Guid id)
        {
            project.ProjectId = id;

            contractPositions.Values.SelectMany(v => v).ToList().ForEach(p =>
            {
                p.ProjectId = id;
                p.Project.ProjectId = id;
            });

            return this;
        }

        /// <summary>
        /// Add a random contract without any positions.
        /// </summary>
        public FusionTestProjectBuilder WithContract() => WithContract(builder => { } );
        public FusionTestProjectBuilder WithContractAndPositions() => WithContract(builder => builder.WithPositions() );

        public FusionTestProjectBuilder WithContract(Action<FusionTestContractBuilder> contractSetup)
        {
            var contractBuilder = new FusionTestContractBuilder(project);
            contractSetup(contractBuilder);

            contracts.Add(contractBuilder.Contract);
            contractPositions[contractBuilder.Contract.Id] = contractBuilder.Positions.ToList();

            return this;
        }


        public FusionTestProjectBuilder WithPositions(int count) => WithPositions(count, count);
        public FusionTestProjectBuilder WithPositions(int min = 3, int max = 10)
        {
            var count = faker.Random.Int(min, max);
            var newPositions = OrgTestData.Position().Generate(count);

            positions.AddRange(newPositions);

            return this;
        }

        public FusionTestProjectBuilder AddToMockService()
        {
            OrgServiceMock.AddProject(this);
            return this;
        }
    }
}
