using Bogus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Testing.Mocks.OrgService
{
    public class FusionTestProjectBuilder
    {

        private readonly ApiProjectV2 project;
        private readonly List<ApiPositionV2> positions = new();
        private readonly Faker faker = new();

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
                    p.Project = new ApiProjectReference()
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

        public FusionTestProjectBuilder WithProjectId(Guid id)
        {
            project.ProjectId = id;

            
            return this;
        }

        public FusionTestProjectBuilder WithDomainId(string domainId)
        {
            project.DomainId = domainId;
            return this;
        }

        public FusionTestProjectBuilder WithProperty(string key, object value)
        {
            if (project.Properties is null)
                project.Properties = new Dictionary<string, object>();

            project.Properties[key] = value;
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

        public FusionTestProjectBuilder SetTaskOwner(Guid position, Guid taskOwnerPosition)
        {
            OrgServiceMock.SetTaskOwner(position, taskOwnerPosition);
            return this;
        }

        public ApiBasePositionV2 AddBasePosition(string name, Action<ApiBasePositionV2> setup = null)
        {
            var bp = new ApiBasePositionV2()
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            setup?.Invoke(bp);

            PositionBuilder.AddBaseposition(bp.JsonClone());
            return bp;
        }

        public ApiPositionV2 AddPosition()
        {
            var position = PositionBuilder.NewPosition();

            position.ProjectId = project.ProjectId;
            position.Project = new ApiProjectReference
            {
                DomainId = project.DomainId,
                Name = project.Name,
                ProjectId = project.ProjectId,
                ProjectType = project.ProjectType
            };

            var clone = position.JsonClone();
            OrgServiceMock.positions.Add(clone);

            return clone;
        }

        public ApiPositionV2 AddPosition(ApiPositionV2 position)
        {
            position.ProjectId = project.ProjectId;
            position.Project = new ApiProjectReference
            {
                DomainId = project.DomainId,
                Name = project.Name,
                ProjectId = project.ProjectId,
                ProjectType = project.ProjectType
            };

            var clone = position.JsonClone();
            OrgServiceMock.positions.Add(clone);

            return clone;
        }
    }
}
