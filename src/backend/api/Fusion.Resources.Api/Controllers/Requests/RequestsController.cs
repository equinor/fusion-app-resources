using Bogus;
using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Domain;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class RequestsController : ResourceControllerBase
    {

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnelRequest>>> GetContractRequests([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            #region Bogus data
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
                    p.Disciplines = Enumerable.Range(0, f.Random.Number(1, 4)).Select(i => new ApiPersonnelDiscipline(f.Hacker.Adjective())).ToList();
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
                .RuleFor(p => p.Updated, f => f.PickRandom(new[] { (DateTime?)null, f.Date.Past().ToUniversalTime() }))
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
                .RuleFor(p => p.Updated, f => f.PickRandom(new[] { (DateTime?)null, f.Date.Past().ToUniversalTime() }))
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

            #endregion


            var realRequests = await DispatchAsync(GetContractPersonnelRequests.QueryContract(projectIdentifier.ProjectId, contractIdentifier));

            return new ApiCollection<ApiContractPersonnelRequest>(realRequests.Select(r => new ApiContractPersonnelRequest(r)).Union(requests));
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> GetContractRequestById([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {

            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));
            return new ApiContractPersonnelRequest(request);
        }


        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> CreatePersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPersonnelRequestRequest request)
        {
            using (var scope = await BeginTransactionAsync())
            {
                var createCommand = new Logic.Commands.ContractorPersonnelRequest.Create(projectIdentifier.ProjectId, contractIdentifier, request.Person)
                    .WithPosition(request.Position.BasePosition.Id, request.Position.Name, request.Position.AppliesFrom, request.Position.AppliesTo, request.Position.Workload)
                    .WithTaskOwner(request.Position.TaskOwner?.PositionId)
                    .WithDescription(request.Description);

                var query = await DispatchAsync(createCommand);

                await scope.CommitAsync();

                return new ApiContractPersonnelRequest(query);
            }
        }


        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> UpdatePersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] ContractPersonnelRequestRequest request)
        {
            using (var scope = await BeginTransactionAsync())
            {
                var query = await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Update(requestId)
                    .SetPerson(request.Person)
                    .SetDescription(request.Description)
                    .SetTaskOwner(request.Position.TaskOwner?.PositionId)
                    .SetPosition(request.Position.BasePosition.Id, request.Position.Name, request.Position.AppliesFrom, request.Position.AppliesTo, request.Position.Workload));

                await scope.CommitAsync();

                return new ApiContractPersonnelRequest(query);
            }
        }



        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> ApproveContractorPersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Approve(request.Id));
                
                await scope.CommitAsync();
            }

            request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(request);
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/reject")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> RejectContractorPersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Reject(request.Id));

                await scope.CommitAsync();
            }

            request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(request);
        }


        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> DeleteContractorRequestById([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Delete(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/provision")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> ProvisionContractorRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Provision(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }


        #region Options
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult> CheckAccessCreateRequests(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "GET,POST");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }


        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult> CheckAccessUpdateRequest(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "GET,PUT,DELETE");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/actions/{actionName}")]
        public async Task<ActionResult> CheckAccessRequestAction(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "POST");
            else
                Response.Headers.Add("Allow", "");

            return NoContent();
        }
        #endregion


        
    }

}
