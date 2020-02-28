using Bogus;
using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Integration;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class RequestsController : ResourceControllerBase
    {

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnelRequest>>> GetContractRequests(string projectIdentifier, string contractIdentifier)
        {

            var persons = new Faker<ApiPerson>()
                .RuleFor(p => p.AzureUniquePersonId, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.PhoneNumber, f => f.Person.Phone)
                .RuleFor(p => p.AccountType, f => f.PickRandomWithout<FusionAccountType>(FusionAccountType.Application))
                .Generate(5);

            var personnel = new Faker<ApiContractPersonnel>()
                .RuleFor(p => p.AzureUniquePersonId, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.PhoneNumber, f => f.Person.Phone)
                .RuleFor(p => p.HasCV, f => f.Random.Bool())
                .RuleFor(p => p.AzureAdStatus, f => f.PickRandomWithout<ApiContractPersonnel.ApiAccountStatus>(ApiContractPersonnel.ApiAccountStatus.NoAccount))
                .FinishWith((f, p) =>
                {
                    p.Disciplines = Enumerable.Range(0, f.Random.Number(1, 4)).Select(i => new PersonnelDiscipline { Name = f.Hacker.Adjective() }).ToList();
                })
                .Generate(30);

            var faker = new Faker();
            var contract = new ApiContractReference
            {
                Company = new ApiCompany { Id = Guid.NewGuid(), Name = faker.Company.CompanyName(), Identifier = faker.Commerce.Department().ToLower() },
                ContractNumber = faker.Finance.Account(10),
                Id = Guid.NewGuid(),
                Name = faker.Lorem.Sentence(faker.Random.Int(4, 10))
            };
            var project = new ApiProjectReference
            {
                Name = faker.Lorem.Sentence(faker.Random.Int(4, 10)),
                Id = Guid.NewGuid(),
                ProjectMasterId = Guid.NewGuid()
            };

            var comments = new Faker<ApiRequestComment>()
                .RuleFor(c => c.Content, f => f.Lorem.Text())
                .RuleFor(p => p.Created, f => f.Date.Past())
                .RuleFor(p => p.Updated, f => f.PickRandom(new[] { (DateTime?)null, f.Date.Past() }))
                .RuleFor(p => p.CreatedBy, f => f.PickRandom(persons))
                .FinishWith((f, c) =>
                {
                    if (c.Updated.HasValue)
                    {
                        c.UpdatedBy = f.PickRandom(persons);
                    }
                })
                .Generate(20);

            var requests = new Faker<ApiContractPersonnelRequest>()
                .RuleFor(p => p.Id, Guid.NewGuid())
                .RuleFor(p => p.Created, f => f.Date.Past())
                .RuleFor(p => p.Updated, f => f.PickRandom(new[] { (DateTime?)null, f.Date.Past() }))
                .RuleFor(p => p.CreatedBy, f => f.PickRandom(persons))
                .RuleFor(p => p.State, f => f.PickRandom<ApiContractPersonnelRequest.ApiRequestState>())
                .RuleFor(p => p.Description, f => f.Lorem.Text())
                .RuleFor(p => p.Contract, contract)
                .RuleFor(p => p.Project, project)
                .RuleFor(p => p.Person, f => f.PickRandom(personnel))
                .RuleFor(p => p.Comments, f => f.PickRandom(comments, f.Random.Number(0, 10)).ToList())
                .FinishWith((f, c) =>
                {
                    if (c.Updated.HasValue)
                    {
                        c.UpdatedBy = f.PickRandom(persons);
                    }
                })
                .Generate(faker.Random.Number(10, 40));

            return new ApiCollection<ApiContractPersonnelRequest>(requests);
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> GetContractRequestById(string projectIdentifier, string contractIdentifier, Guid requestId)
        {
            var requests = await GetContractRequests(null, null);

            return requests.Value.Value.First();
        }


        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> CreatePersonnelRequest(string projectIdentifier, string contractIdentifier, [FromBody] ContractPersonnelRequestRequest request)
        {

            var profile = UserFusionProfile;
            if (profile is null)
                return BadRequest(new { message = "Cannot find profile for current user" });

            var personnel = new Faker<ApiContractPersonnel>()
                .RuleFor(p => p.AzureUniquePersonId, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.PhoneNumber, f => f.Person.Phone)
                .RuleFor(p => p.HasCV, f => f.Random.Bool())
                .RuleFor(p => p.AzureAdStatus, f => f.PickRandomWithout<ApiContractPersonnel.ApiAccountStatus>(ApiContractPersonnel.ApiAccountStatus.NoAccount))
                .FinishWith((f, p) =>
                {
                    p.Disciplines = Enumerable.Range(0, f.Random.Number(1, 4)).Select(i => new PersonnelDiscipline { Name = f.Hacker.Adjective() }).ToList();
                })
                .Generate();
            var faker = new Faker();
            var contract = new ApiContractReference
            {
                Company = new ApiCompany { Id = Guid.NewGuid(), Name = faker.Company.CompanyName(), Identifier = faker.Commerce.Department().ToLower() },
                ContractNumber = faker.Finance.Account(10),
                Id = Guid.NewGuid(),
                Name = faker.Lorem.Sentence(faker.Random.Int(4, 10))
            };
            var project = new ApiProjectReference
            {
                Name = faker.Lorem.Sentence(faker.Random.Int(4, 10)),
                Id = Guid.NewGuid(),
                ProjectMasterId = Guid.NewGuid()
            };


            var createdItem = new ApiContractPersonnelRequest
            {
                Id = Guid.NewGuid(),
                State = ApiContractPersonnelRequest.ApiRequestState.Created,
                Description = request.Description,
                Created = DateTime.UtcNow,
                CreatedBy = new ApiPerson(profile),
                Position = new ApiRequestPosition()
                {
                    AppliesFrom = request.Position.AppliesFrom,
                    AppliesTo = request.Position.AppliesTo,
                    BasePosition = new ApiRequestBasePosition() { Id = request.Position.BasePosition.Id, Name = "Test position" },
                    Name = request.Position.Name,
                    ExternalId = "123",
                    TaskOwner = request.Position.TaskOwner != null ? new ApiRequestTaskOwner { PositionId = request.Position.TaskOwner.PositionId } : null,
                    Id = request.Position.Id,
                },
                Person = personnel,
                Comments = new List<ApiRequestComment>(),
                Contract = contract,
                Project = project
            };

            return createdItem;
        }

    }


    public class ContractPersonnelRequestRequest
    {
        public Guid? Id { get; set; }
        public string Description { get; set; }

        public RequestPosition Position { get; set; }
        public RequestPerson Person { get; set; }
    

        public class RequestPosition
        {
            /// <summary>
            /// Existing org chart position id.
            /// </summary>
            public Guid? Id { get; set; }

            public BasePosition BasePosition { get; set; }
            public string Name { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string Obs { get; set; }
            
            public TaskOwner TaskOwner { get; set; }
        }

        public class TaskOwner
        {
            /// <summary>
            /// The position id is nullable, as at a later date other ways of referencing an un-provisioned request will be made available.
            /// </summary>
            public Guid? PositionId { get; set; }
        }

        public class BasePosition
        {
            public Guid Id { get; set; }
        }
    
        public class RequestPerson
        {
            public Guid? AzureUniquePersonId { get; set; }
            public string Mail { get; set; }

        }
    }

    

    

    public class ApiContractPersonnelRequest
    {
        public Guid Id { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson UpdatedBy { get; set; }
        public ApiRequestState State { get; set; }
        public string Description { get; set; }

        public ApiRequestPosition Position { get; set; }
        public ApiContractPersonnel Person { get; set; }

        public ApiContractReference Contract { get; set; }
        public ApiProjectReference Project { get; set; }

        public List<ApiRequestComment> Comments { get; set; }

        public enum ApiRequestState { Created, Submitted, Approved, Rejected, Provisioned }
    }

}
