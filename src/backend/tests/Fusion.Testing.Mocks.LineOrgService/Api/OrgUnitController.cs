using Fusion.AspNetCore.OData;
using Fusion.Integration.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    [ApiVersion("1.0")]
    [ApiController]
    public class OrgUnitController : ControllerBase
    {

        [HttpGet("/org-units/")]
        public ActionResult<ApiPagedCollection<ApiOrgUnit>> GetOrgUnits( [FromQuery] ODataQueryParams query)
        {

            return new ApiPagedCollection<ApiOrgUnit>(LineOrgServiceMock.OrgUnits.ToArray().ToList(), LineOrgServiceMock.OrgUnits.Count());
        }

            [HttpGet("/org-units/{orgUnitId}")]
        public ActionResult<ApiOrgUnit> GetOrgUnit([FromRoute] string orgUnitId, [FromQuery] ODataQueryParams query)
        {
            var departmentId = DepartmentId.FromFullPath(orgUnitId);
            var parts = orgUnitId.Split(' ');
            var orgUnit = new ApiOrgUnit
            {
                BusinessArea = new ApiOrgUnitRef { Name = departmentId.BusinessArea, ShortName = departmentId.BusinessArea },
                Department = departmentId.LocalPath,
                FullDepartment = departmentId.FullPath,
                ParentPath = new ApiOrgUnit.OrgUnitParentPath
                {
                    Level0 = (parts.Length > 0) ? parts[0] : null,
                    Level1 = (parts.Length > 1) ? parts[1] : null,
                    Level2 = (parts.Length > 2) ? parts[2] : null,
                    Level3 = (parts.Length > 3) ? parts[3] : null,
                    Level4 = (parts.Length > 4) ? parts[4] : null,
                    Level5 = (parts.Length > 5) ? parts[5] : null,
                    Level6 = (parts.Length > 6) ? parts[6] : null,
                    Level7 = (parts.Length > 7) ? parts[7] : null,
                    Level8 = (parts.Length > 8) ? parts[8] : null
                }
            };
            return Ok(orgUnit);
        }

    }
}
