using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class PersonnelController : ResourceControllerBase
    {

        public PersonnelController()
        {
        }

        [HttpGet("resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiExternalPersonnelPerson>>> GetPersonnel([FromQuery]ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            if (query is null) query = new ODataQueryParams { Top = 1000 };
            if (query.Top > 1000) return ApiErrors.InvalidPageSize("Max page size is 1000");

            var externalPersonell = await DispatchAsync(new GetExternalPersonnel(query));
            var apiModelItems = externalPersonell.Select(ep => new ApiExternalPersonnelPerson(ep));

            return new ApiCollection<ApiExternalPersonnelPerson>(apiModelItems);
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnel>>> GetContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery]ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var contractPersonnel = await DispatchAsync(new GetContractPersonnel(contractIdentifier, query));

            var returnItems = contractPersonnel.Select(p => new ApiContractPersonnel(p));

            var collection = new ApiCollection<ApiContractPersonnel>(returnItems);
            return collection;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiContractPersonnel>> GetContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnelId = new PersonnelId(personIdentifier);

            var contractPersonnel = await DispatchAsync(new GetContractPersonnelItem(contractIdentifier, personnelId));

            if (contractPersonnel == null)
            {
                return FusionApiError.NotFound(personIdentifier, "Could not locate personnel");
            }

            var returnItem = new ApiContractPersonnel(contractPersonnel);
            return returnItem;
        }

        [HttpPost("resources/personnel/{personIdentifier}/refresh")]
        public async Task<ActionResult<ApiExternalPersonnelPerson>> RefreshPersonnel(string personIdentifier)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await DispatchAsync(new RefreshPersonnel(personIdentifier));
                var refreshedPersonnel = await DispatchAsync(new GetExternalPersonnelPerson(personIdentifier));

                return new ApiExternalPersonnelPerson(refreshedPersonnel);
            }
            catch (PersonNotFoundError)
            {
                return ApiErrors.NotFound($"Personnel with given id not found", "resources/personnel/{personIdentifier}");
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiContractPersonnel>> CreateContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] CreateContractPersonnelRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var createCommand = new CreateContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, request.Mail);
            request.LoadCommand(createCommand);

            try
            {
                using (var scope = await BeginTransactionAsync())
                {

                    var newPersonnel = await DispatchAsync(createCommand);
                    await scope.CommitAsync();

                    var item = new ApiContractPersonnel(newPersonnel);
                    return Created($"/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{item.Mail}", item);
                }
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel-collection")]
        public async Task<ActionResult<ApiBatchResponse<ApiContractPersonnel>>> CreateContractPersonnelBatch([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] IEnumerable<CreateContractPersonnelRequest> requests)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

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

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/refresh")]
        public async Task<ActionResult> RefreshContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnel = await DispatchAsync(new GetContractPersonnel(contractIdentifier));

            foreach (var person in personnel)
            {
                await DispatchAsync(new RefreshPersonnel(person.PersonnelId));
            }

            return Ok();
        }


        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiContractPersonnel>> UpdateContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier, [FromBody] UpdateContractPersonnelRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

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
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnelId = new PersonnelId(personIdentifier);

            await DispatchAsync(new DeleteContractPersonnel(projectIdentifier.ProjectId, contractIdentifier, personnelId));

            return NoContent();
        }

    }


}
