using Bogus;
using System;
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
                        AssignedDepartment = f.Address.CityPrefix(),
                        Discipline = f.Address.CountryCode(),
                        AdditionalNote = f.Hacker.Phrase(),
                        Type = ApiAllocationRequestType.Normal,
                        IsDraft = false,
                        ProposedChanges = new ApiPropertiesCollection(),
                        OrgPositionId = Guid.NewGuid(),
                        OrgPositionInstanceId = Guid.NewGuid()
                    };
                    return request;
                }
            );
    }
}
