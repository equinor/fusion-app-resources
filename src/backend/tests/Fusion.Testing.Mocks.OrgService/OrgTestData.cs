using Bogus;
using Fusion.ApiClients.Org;
using System;
using System.Linq;

namespace Fusion.Testing.Mocks.OrgService
{
    public class OrgTestData
    {
        public static Faker<ApiProjectV2> Project() => new Faker<ApiProjectV2>()
            .CustomInstantiator(f =>
            {
                var director = Position()
                    .RuleFor(p => p.BasePosition, f => PositionBuilder.DirectorBasePosition)
                    .Generate();

                var project = new ApiProjectV2()
                {
                    ProjectId = Guid.NewGuid(),
                    Name = f.Commerce.ProductName(),
                    ProjectType = "PRD",
                    DomainId = f.Random.AlphaNumeric(5),
                    Dates = new ApiProjectDatesV2() { EndDate = f.Date.Future(), StartDate = f.Date.Past(), Gates = new ApiProjectDecisionGatesV2() { } },
                    Director = director,
                    DirectorPositionId = director.Id
                };

                director.ProjectId = project.ProjectId;
                director.Project = new ApiProjectReferenceV2()
                {
                    ProjectId = project.ProjectId,
                    DomainId = project.DomainId,
                    Name = project.Name,
                    ProjectType = project.ProjectType
                };

                return project;
            });

        public static Faker<ApiPositionV2> Position() => PositionBuilder.CreateTestPosition();

        public static Faker<ApiProjectContractV2> Contract() => new Faker<ApiProjectContractV2>()
            .CustomInstantiator(f =>
            {
                return new ApiProjectContractV2()
                {
                    Id = Guid.NewGuid(),
                    Company = Company().Generate(),
                    ContractNumber = f.Finance.Account(10),
                    Description = f.Lorem.Paragraphs(),
                    StartDate = f.Date.Past(),
                    EndDate = f.Date.Future(),
                    Name = f.Hacker.Phrase()
                };
            });

        public static Faker<ApiCompanyV2> Company() => new Faker<ApiCompanyV2>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.Name, f => f.Company.CompanyName());
    }
}
