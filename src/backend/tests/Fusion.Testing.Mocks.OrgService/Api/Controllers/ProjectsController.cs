using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.ApiClients.Org;
using Fusion.AspNetCore.Api;

namespace Fusion.Testing.Mocks.OrgService.Api.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class ProjectsController : ControllerBase
    {
        [HttpGet("/projects/{projectIdentifier}")]
        public ActionResult<ApiProjectV2> GetProject([FromRoute] ProjectIdentifier projectIdentifier)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project != null)
                return project;

            return NotFound();
        }

        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public ActionResult<List<ApiProjectContractV2>> GetContracts([FromRoute] ProjectIdentifier projectIdentifier)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project is null)
                return NotFound(new { error = new { message = "Could not locate project" } });

            if (!OrgServiceMock.contracts.TryGetValue(project.ProjectId, out List<ApiProjectContractV2> contracts))
                contracts = new List<ApiProjectContractV2>();

            return contracts;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractId}")]
        public ActionResult<ApiProjectContractV2> GetContract([FromRoute] ProjectIdentifier projectIdentifier, Guid contractId)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project is null)
                return NotFound(new { error = new { message = "Could not locate project" } });

            if (!OrgServiceMock.contracts.TryGetValue(project.ProjectId, out List<ApiProjectContractV2> contracts))
                contracts = new List<ApiProjectContractV2>();

            var contract = contracts.FirstOrDefault(c => c.Id == contractId);
            
            if (contract is null)
                return NotFound(new { error = new { message = "Could not locate contract" } });

            return contract;
        }



        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public ActionResult<ApiProjectContractV2> CreateContract([FromRoute] ProjectIdentifier projectIdentifier, NewContractRequest request)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project is null)
                return NotFound(new { error = new { message = "Could not locate project" } });

            var company = request.CompanyId == null ? null : OrgServiceMock.companies.FirstOrDefault(c => c.Id == request.CompanyId);
            var contract = new ApiProjectContractV2()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                ContractNumber = request.ContractNumber,
                Company = company,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            if (!OrgServiceMock.contracts.TryGetValue(project.ProjectId, out List<ApiProjectContractV2> contracts))
            {
                OrgServiceMock.contracts[project.ProjectId] = new List<ApiProjectContractV2>() { contract };
            } 
            else
            {
                contracts.Add(contract);
            }

            return contract;
        }

        [HttpPatch("/projects/{projectIdentifier}/contracts/{contractId}")]
        public ActionResult<ApiProjectContractV2> PatchContract([FromRoute] ProjectIdentifier projectIdentifier, Guid contractId, [FromBody]PatchContractRequest request)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project is null)
                return NotFound(new { error = new { message = "Could not locate project" } });


            if (!OrgServiceMock.contracts.TryGetValue(project.ProjectId, out List<ApiProjectContractV2> projectContracts))
                projectContracts = new List<ApiProjectContractV2>();

            var contract = projectContracts.FirstOrDefault(c => c.Id == contractId);

            if (request.CompanyId.HasValue)
            {
                var company = request.CompanyId.Value == null ? null : OrgServiceMock.companies.FirstOrDefault(c => c.Id == request.CompanyId.Value);
                contract.Company = company;
            }

            if (request.Name.HasValue)
                contract.Name = request.Name.Value;


            if (request.ContractNumber.HasValue)
                contract.ContractNumber = request.ContractNumber.Value;

            if (request.Description.HasValue)
                contract.Description = request.Description.Value;

            if (request.StartDate.HasValue)
                contract.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                contract.EndDate = request.EndDate.Value;

            if (request.CompanyRepId.HasValue)
                contract.CompanyRep = FindContractPosition(request.CompanyRepId.Value);

            if (request.ContractRepId.HasValue)
                contract.ContractRep = FindContractPosition(request.ContractRepId.Value);

            if (request.ExternalCompanyRepId.HasValue)
                contract.ExternalCompanyRep = FindContractPosition(request.ExternalCompanyRepId.Value);

            if (request.ExternalContractRepId.HasValue)
                contract.ExternalContractRep = FindContractPosition(request.ExternalContractRepId.Value);

            return contract;
        }

        private static ApiPositionV2 FindContractPosition(Guid? positionId)
        {
            if (positionId is null) return null;

            OrgServiceMock.contractPositions.TryGetValue(positionId.Value, out ApiPositionV2 pos);
            return pos;
        }

        public class NewContractRequest
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string ContractNumber { get; set; }

            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            public Guid? CompanyRepPositionId { get; set; }
            public Guid? ContractRepPositionId { get; set; }
            public Guid? CompanyId { get; set; }
        }

        public class PatchContractRequest : PatchRequest
        {
            public PatchProperty<string> Name { get; set; }
            public PatchProperty<string> ContractNumber { get; set; }
            public PatchProperty<string> Description { get; set; }

            public PatchProperty<DateTime?> StartDate { get; set; }
            public PatchProperty<DateTime?> EndDate { get; set; }

            public PatchProperty<Guid?> CompanyId { get; set; }
            public PatchProperty<Guid?> CompanyRepId { get; set; }
            public PatchProperty<Guid?> ContractRepId { get; set; }

            public PatchProperty<Guid?> ExternalCompanyRepId { get; set; }
            public PatchProperty<Guid?> ExternalContractRepId { get; set; }
        }
    }
}
