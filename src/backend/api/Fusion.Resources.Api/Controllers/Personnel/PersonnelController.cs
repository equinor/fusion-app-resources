using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bogus;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class PersonnelController : ResourceControllerBase
    {

        public PersonnelController()
        {
        }
        
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnel>>> GetContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery]ODataQueryParams query) 
        {
            var contractPersonnel = await DispatchAsync(new GetContractPersonnel(contractIdentifier, query));

            var returnItems = contractPersonnel.Select(p => new ApiContractPersonnel(p));

            var collection = new ApiCollection<ApiContractPersonnel>(returnItems);
            return collection;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiContractPersonnel>> GetContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            var personnelId = new PersonnelId(personIdentifier);

            var contractPersonnel = await DispatchAsync(new GetContractPersonnelItem(contractIdentifier, personnelId));

            if (contractPersonnel == null)
            {
                return FusionApiError.NotFound(personIdentifier, "Could not locate personnel");
            }

            var returnItem = new ApiContractPersonnel(contractPersonnel);
            return returnItem;
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiContractPersonnel>> CreateContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] CreateContractPersonnelRequest request)
        {

            var createCommand = new CreateContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, request.Mail);
            request.LoadCommand(createCommand);

            using (var scope = await BeginTransactionAsync())
            {
                var newPersonnel = await DispatchAsync(createCommand);
                
                await scope.CommitAsync();

                var item = new ApiContractPersonnel(newPersonnel);
                return Created($"/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{item.Mail}", item);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel-collection")]
        public async Task<ActionResult<ApiBatchResponse<ApiContractPersonnel>>> CreateContractPersonnelBatch([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] IEnumerable<CreateContractPersonnelRequest> requests)
        {
            var editor = User.GetAzureUniqueIdOrThrow();
            var itemsToCreate = requests.ToList();

            var results = new List<ApiBatchItemResponse<ApiContractPersonnel>>();

            foreach (var request in requests)
            {
                var createCommand = new CreateContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, request.Mail);
                request.LoadCommand(createCommand);

                using (var scope = await BeginTransactionAsync())
                {
                    try
                    {
                        var newPersonnel = await DispatchAsync(createCommand);
                        await scope.CommitAsync();
                        results.Add(new ApiBatchItemResponse<ApiContractPersonnel>(new ApiContractPersonnel(newPersonnel), HttpStatusCode.Created));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ApiBatchItemResponse<ApiContractPersonnel>(HttpStatusCode.BadRequest, ex.Message));
                        await scope.RollbackAsync();
                    }
                }
            }

            return new ApiBatchResponse<ApiContractPersonnel>(results);
        }


        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiContractPersonnel>> UpdateContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier, [FromBody] UpdateContractPersonnelRequest request)
        {

            var updateCommand = new UpdateContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, personIdentifier);
            request.LoadCommand(updateCommand);

            using (var scope = await BeginTransactionAsync())
            {
                var updatedPersonnel = await DispatchAsync(updateCommand);

                await scope.CommitAsync();

                var item = new ApiContractPersonnel(updatedPersonnel);
                return item;
            }
        }


        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult> DeleteContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            var personnelId = new PersonnelId(personIdentifier);

            await DispatchAsync(new DeleteContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, personnelId));

            return NoContent();
        }
    
    }


}
