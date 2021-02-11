using Bogus;
using System;
using Fusion.ApiClients.Org;
using Fusion.Resources.Api.Controllers;

namespace Fusion.Testing.Mocks
{
    public class ResourceAllocationTestData
    {
        public static Faker<CreateResourceAllocationRequest> Request() => new Faker<CreateResourceAllocationRequest>()
            .CustomInstantiator(f =>
                {
                    var request = new CreateResourceAllocationRequest()
                    {
                        Id = Guid.NewGuid(),
                        Discipline = f.Hacker.Phrase(),
                        AdditionalNote = f.Hacker.Phrase(),
                        Type = ApiAllocationRequestType.Normal,
                        IsDraft = false,
                        ProposedChanges = new ApiPropertiesCollection(),
                        OrgPositionId = Guid.NewGuid(),
                        OrgPositionInstance = new ApiPositionInstance
                        {
                            Id = Guid.NewGuid(),
                            AppliesFrom = f.Date.Past(),
                            AppliesTo = f.Date.Future(),
                            Workload = f.Random.Double(0, 100),
                            LocationId = f.Random.Guid(),
                            Obs = f.Hacker.Adjective()
                        }
                    };
                    return request;
                }
            );
    }
}
