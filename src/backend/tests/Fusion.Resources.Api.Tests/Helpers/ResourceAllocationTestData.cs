using Bogus;
using System;
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
                    };
                    return request;
                }
            );
    }
}
