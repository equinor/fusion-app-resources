using Bogus;
using System;
using System.Collections.Generic;
using Fusion.Services.Org.ApiModels;

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
                    Dates = new ApiProjectDates() { EndDate = f.Date.Future(), StartDate = f.Date.Past(), Gates = new ApiProjectDecisionGates() { } },
                    Director = director,
                    DirectorPositionId = director.Id,
                    Properties = new Dictionary<string, object>()
                };

                director.ProjectId = project.ProjectId;
                director.Project = new ApiProjectReference()
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

        public static Faker<ApiCompany> Company() => new Faker<ApiCompany>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.Name, f => f.Company.CompanyName());
    }

}
