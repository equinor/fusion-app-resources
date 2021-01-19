using Bogus;
using System;
using System.Collections.Generic;
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
                        Type = ApiResourceAllocationRequest.ApiAllocationRequestType.Normal,
                        IsDraft = false,
                        ProposedChanges = new List<ApiProposedChange>(),
                        OrgPositionInstance = new ApiPositionInstance
                        {
                            AppliesFrom = f.Date.Past(),
                            AppliesTo = f.Date.Future(),
                            Workload = f.Random.Double(0,100),
                            Location = f.Address.City(),
                            Obs = f.Hacker.Adjective()
                        }
                    };
                    return request;
                }
            );
    }
}
