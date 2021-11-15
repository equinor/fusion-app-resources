using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain.Commands;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public partial class InternalPersonnelController
    {
        [MapToApiVersion("1.0-preview")]
        [HttpGet("/projects/{projectIdentifier}/resources/persons")]
        [Obsolete]
        public async Task<ActionResult> SearchPreview([FromRoute] PathProjectIdentifier? projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (projectIdentifier is not null)
                        or.OrgChartReadAccess(projectIdentifier.ProjectId);

                    or.BeResourceOwner();
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new SearchAllPersonnel(query.Search);
            if (query.HasFilter)
            {
                var departmentFilter = query.Filter.GetFilterForField("department");
                if (departmentFilter.Operation != FilterOperation.Eq)
                    return FusionApiError.InvalidOperation("InvalidQueryFilter", "Only the 'eq' operator is supported.");

                command = command.WithDepartmentFilter(departmentFilter.Value);
            }
            var result = await DispatchAsync(command);

            return Ok(result.Select(x => ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(x)));
        }
    }
}
