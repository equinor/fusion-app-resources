using Bogus;
using System;
using Fusion.ApiClients.Org;
using Fusion.Resources.Api.Controllers;

namespace Fusion.Testing.Mocks
{
    public class ResourceAllocationTestData
    {
        public static Faker<CreateProjectAllocationRequest> Request() => new Faker<CreateProjectAllocationRequest>()
            .CustomInstantiator(f =>
                {
                    var request = new CreateProjectAllocationRequest()
                    {
                        Id = Guid.NewGuid(),
                        Discipline = f.Hacker.Phrase(),
                        AdditionalNote = f.Hacker.Phrase(),
                        Type = ApiAllocationRequestType.Normal,
                        IsDraft = false,
                        ProposedChanges = new ApiPropertiesCollection(),
                        OrgPositionId = Guid.NewGuid(),
                        OrgPositionInstance = new ApiPositionInstanceV2
                        {
                            AppliesFrom = f.Date.Past(),
                            AppliesTo = f.Date.Future(),
                            Workload = f.Random.Double(0, 100),
                            Location = new ApiPositionLocationV2 { Id = Guid.NewGuid(), Country = f.Address.Country(), Code = f.Address.CountryCode(), Name = f.Address.BuildingNumber() },
                            Obs = f.Hacker.Adjective()
                        }
                    };
                    return request;
                }
            );
    }
}
