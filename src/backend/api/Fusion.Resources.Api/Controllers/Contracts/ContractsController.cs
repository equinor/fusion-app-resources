using Bogus;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IOrgApiClientFactory orgApiClientFactory;

        public ContractsController(IMediator mediator, IOrgApiClientFactory orgApiClientFactory)
        {
            this.mediator = mediator;
            this.orgApiClientFactory = orgApiClientFactory;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiCollection<ApiContract>>> GetProjectContracts([FromRoute]ProjectIdentifier projectIdentifier)
        {
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var realContracts = await client.GetContractsV2Async(projectIdentifier.ProjectId);

            var contracts = new Faker<ApiContract>()
                .RuleFor(c => c.ContractNumber, f => f.Finance.Account(10))
                .RuleFor(c => c.Name, f => f.Lorem.Sentence(f.Random.Int(4, 10)))
                .RuleFor(c => c.Company, f => new ApiCompany { Id = Guid.NewGuid(), Name = f.Company.CompanyName() })
                .RuleFor(c => c.Description, f => f.Lorem.Paragraphs())
                .RuleFor(c => c.StartDate, f => f.Date.Past())
                .RuleFor(c => c.EndDate, f => f.Date.Future())
                .Generate(new Random(Guid.NewGuid().GetHashCode()).Next(5, 10));

            var collection = new ApiCollection<ApiContract>(realContracts.Select(c => new ApiContract(c)).Union(contracts));
            return Ok(collection);
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractId}")]
        public async Task<ActionResult<ApiContract>> GetProjectContracts([FromRoute]ProjectIdentifier projectIdentifier, Guid contractId)
        {
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, contractId);

            return new ApiContract(orgContract);
        }

        [HttpGet("/projects/{projectIdentifier}/available-contracts")]
        public async Task<ActionResult<ApiCollection<ApiUnallocatedContract>>> GetProjectAvailableContracts([FromRoute]ProjectIdentifier projectIdentifier)
        {
            var contracts = new[]
            {
                new ApiUnallocatedContract { ContractNumber = "0000000001" },
                new ApiUnallocatedContract { ContractNumber = "0000000002" },
                new ApiUnallocatedContract { ContractNumber = "0000000003" },
                new ApiUnallocatedContract { ContractNumber = "0000000004" },
                new ApiUnallocatedContract { ContractNumber = "0000055555" },
                new ApiUnallocatedContract { ContractNumber = "0000666666" },
                new ApiUnallocatedContract { ContractNumber = "1000000000" },
                new ApiUnallocatedContract { ContractNumber = "1111111111" }
            };

            return Ok(new ApiCollection<ApiUnallocatedContract>(contracts));
        }

        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiContract>> AllocateProjectContract([FromRoute]ProjectIdentifier projectIdentifier, [FromBody] ContractRequest request)
        {
            var allocatedContract = await mediator.Send(new AllocateContract(projectIdentifier.ProjectId, request.ContractNumber));
            allocatedContract = await mediator.Send(new UpdateContract(projectIdentifier.ProjectId, allocatedContract.OrgContractId)
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = request.Company?.Id,
                Description = request.Description
            });

            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, allocatedContract.OrgContractId);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract));
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}")]
        public async Task<ActionResult<ApiContract>> UpdateProjectContract([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractRequest request)
        {
            await mediator.Send(new UpdateContract(projectIdentifier.ProjectId, contractIdentifier)
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = request.Company?.Id,
                Description = request.Description
            });
           
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, contractIdentifier);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract));
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-company-representative")]
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> CreateContractExternalCompanyRep([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);

            var createPositionMessage = new HttpRequestMessage(HttpMethod.Post, $"/projects/{projectIdentifier.ProjectId}/contracts/{contractIdentifier}/positions");
            createPositionMessage.Content = new StringContent(JsonConvert.SerializeObject(new ApiClients.Org.ApiPositionV2
            {
                BasePosition = new ApiClients.Org.ApiBasePositionV2 { Id = request.BasePosition.Id },
                Name = request.Name,
                ExternalId = "external-comp-rep",
                Instances = new List<ApiClients.Org.ApiPositionInstanceV2>
                {
                    new ApiClients.Org.ApiPositionInstanceV2
                    {
                        AppliesFrom = request.AppliesFrom,
                        AppliesTo = request.AppliesTo,
                        Workload = request.Workload,
                        AssignedPerson = request.AssignedPerson == null ? null : new ApiClients.Org.ApiPersonV2 { AzureUniqueId = request.AssignedPerson.AzureUniquePersonId, Mail = request.AssignedPerson.Mail }
                    }
                }
            }), Encoding.UTF8, "application/json");
            var resp = await client.SendAsync(createPositionMessage);
            var responseContent = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                var newPosition = JsonConvert.DeserializeObject<ApiClients.Org.ApiPositionV2>(responseContent);

                // Update the rep
                await mediator.Send(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier) { CompanyRepPositionId = newPosition.Id });
                
                return newPosition;
            }

            if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return BadRequest(JsonDocument.Parse(responseContent));

            return new ObjectResult(JsonDocument.Parse(responseContent))
            {
                StatusCode = (int)resp.StatusCode
            };
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-contract-responsible")]
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> CreateContractExternalContractResp([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {

            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);

            var createPositionMessage = new HttpRequestMessage(HttpMethod.Post, $"/projects/{projectIdentifier.ProjectId}/contracts/{contractIdentifier}/positions");
            createPositionMessage.Content = new StringContent(JsonConvert.SerializeObject(new ApiClients.Org.ApiPositionV2
            {
                BasePosition = new ApiClients.Org.ApiBasePositionV2 { Id = request.BasePosition.Id },
                Name = request.Name,
                ExternalId = "external-contract-resp",
                Instances = new List<ApiClients.Org.ApiPositionInstanceV2>
                {
                    new ApiClients.Org.ApiPositionInstanceV2
                    {
                        AppliesFrom = request.AppliesFrom,
                        AppliesTo = request.AppliesTo,
                        Workload = request.Workload,
                        AssignedPerson = request.AssignedPerson == null ? null : new ApiClients.Org.ApiPersonV2 { AzureUniqueId = request.AssignedPerson.AzureUniquePersonId, Mail = request.AssignedPerson.Mail }
                    }
                }
            }), Encoding.UTF8, "application/json");
            var resp = await client.SendAsync(createPositionMessage);
            var responseContent = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                var newPosition = JsonConvert.DeserializeObject<ApiClients.Org.ApiPositionV2>(responseContent);

                // Update the rep
                await mediator.Send(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier) { ContractResponsiblePositionId = newPosition.Id });

                return newPosition;
            }

            if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return BadRequest(JsonDocument.Parse(responseContent));

            return new ObjectResult(JsonDocument.Parse(responseContent))
            {
                StatusCode = (int)resp.StatusCode
            };
        }
    }
}
