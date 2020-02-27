using Bogus;
using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ContractsController : ControllerBase
    {

        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiCollection<ApiContract>>> GetProjectContracts(string projectIdentifier)
        {
            var contracts = new Faker<ApiContract>()
                .RuleFor(c => c.ContractNumber, f => f.Finance.Account(10))
                .RuleFor(c => c.Name, f => f.Lorem.Sentence(f.Random.Int(4, 10)))
                .RuleFor(c => c.Company, f => new ApiCompany { Id = Guid.NewGuid(), Name = f.Company.CompanyName() })
                .RuleFor(c => c.Description, f => f.Lorem.Paragraphs())
                .RuleFor(c => c.StartDate, f => f.Date.Past())
                .RuleFor(c => c.EndDate, f => f.Date.Future())
                .Generate(new Random(Guid.NewGuid().GetHashCode()).Next(5, 10));

            var collection = new ApiCollection<ApiContract>(contracts);
            return Ok(collection);
        }

        [HttpGet("/projects/{projectIdentifier}/available-contracts")]
        public async Task<ActionResult<ApiCollection<ApiUnallocatedContract>>> GetProjectAvailableContracts(string projectIdentifier)
        {
            var contracts = new Faker<ApiUnallocatedContract>()
                .RuleFor(c => c.ContractNumber, f => f.Finance.Account(10))
                .Generate(new Random(Guid.NewGuid().GetHashCode()).Next(5, 10));

            return Ok(new ApiCollection<ApiUnallocatedContract>(contracts));
        }

        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiContract>> AllocateProjectContract(string projectIdentifier, [FromBody] ContractRequest request)
        {

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract
            {
                Id = Guid.NewGuid(),
                ContractNumber = request.ContractNumber,
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Company = request.Company != null ? new ApiCompany { Id = Guid.NewGuid(), Identifier = request.Company.Identifier, Name = new Faker().Company.CompanyName() } : null,
                CompanyRepPositionId = request.CompanyRepPositionId,
                ContractResponsiblePositionId = request.ContractResponsiblePositionId,
                ExternalCompanyRepPositionId = request.ExternalCompanyRepPositionId,
                ExternalContractResponsiblePositionId = request.ExternalContractResponsiblePositionId
            });
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}")]
        public async Task<ActionResult<ApiContract>> UpdateProjectContract(string projectIdentifier, string contractIdentifier, [FromBody] ContractRequest request)
        {

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract
            {
                Id = request.Id.GetValueOrDefault(Guid.NewGuid()),
                ContractNumber = request.ContractNumber,
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Company = request.Company != null ? new ApiCompany { Id = Guid.NewGuid(), Identifier = request.Company.Identifier, Name = new Faker().Company.CompanyName() } : null,
                CompanyRepPositionId = request.CompanyRepPositionId,
                ContractResponsiblePositionId = request.ContractResponsiblePositionId,
                ExternalCompanyRepPositionId = request.ExternalCompanyRepPositionId,
                ExternalContractResponsiblePositionId = request.ExternalContractResponsiblePositionId
            });
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-company-representative")]
        public async Task<ActionResult> CreateContractExternalCompanyRep(string projectIdentifier, string contractIdentifier, [FromBody] ContractPositionRequest request)
        {

            return Ok();
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-contract-responsible")]
        public async Task<ActionResult> CreateContractExternalContractResp(string projectIdentifier, string contractIdentifier, [FromBody] ContractPositionRequest request)
        {

            return Ok();
        }
    }

    public class ContractPositionRequest
    {
        public IdEntity BasePosition { get; set; }
        public string Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public PersonRequest AssignedPerson { get; set; }
    }

    public class PersonRequest
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; }
    }

    public class IdEntity
    {
        public Guid Id { get; set; }
    }

    public class ApiContract
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ApiCompany Company { get; set; }
        
        public Guid? ContractResponsiblePositionId { get; set; }
        public Guid? CompanyRepPositionId { get; set; }
        public Guid? ExternalContractResponsiblePositionId { get; set; }
        public Guid? ExternalCompanyRepPositionId { get; set; }
    }

    public class ApiCompany
    {
        public Guid Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
    }


    public class ApiPerson
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string JobTitle { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FusionAccountType AccountType { get; set; }
    }

    public class ContractRequest
    {
        public Guid? Id { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public CompanyRequest Company { get; set; }

        public Guid? ContractResponsiblePositionId { get; set; }
        public Guid? CompanyRepPositionId { get; set; }
        public Guid? ExternalContractResponsiblePositionId { get; set; }
        public Guid? ExternalCompanyRepPositionId { get; set; }
    }

    public class CompanyRequest
    {
        public Guid Id { get; set; }
        public string Identifier { get; set; }
    }
}
