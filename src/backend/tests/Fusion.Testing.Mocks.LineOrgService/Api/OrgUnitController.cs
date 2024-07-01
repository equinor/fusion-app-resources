using Fusion.AspNetCore.OData;
using Fusion.Integration.LineOrg;
using Fusion.Resources;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    [ApiVersion("1.0")]
    [ApiController]
    public class OrgUnitController : ControllerBase
    {
        [HttpGet("/org-units")]
        public ActionResult<ApiPagedCollection<ApiOrgUnit>> GetOrgUnits([FromQuery] ODataQueryParams query)
        {

            // Take a copy of the items, so we do not update items managed by the test.
            var itemsData = JsonConvert.SerializeObject(LineOrgServiceMock.OrgUnits.ToArray());
            var itemsCopy = JsonConvert.DeserializeObject<List<ApiOrgUnit>>(itemsData);

            // ensure expanded properties are added.
            if (query.ShouldExpand("management"))
            {
                itemsCopy.ForEach(i =>
                {
                    if (i.Management is null)
                    {
                        i.Management = new ApiOrgUnitManagement()
                        {
                            Persons = new List<ApiPerson>()
                        };
                    }
                });
            }

            return new ApiPagedCollection<ApiOrgUnit>(itemsCopy, itemsCopy.Count);
        }

        [HttpGet("/org-units/{orgUnitId}")]
        public ActionResult<ApiOrgUnit> GetOrgUnit([FromRoute] string orgUnitId, [FromQuery] ODataQueryParams query)
        {
            var orgUnit = LineOrgServiceMock.OrgUnits.FirstOrDefault(o => o.SapId.EqualsIgnCase(orgUnitId) || o.FullDepartment.EqualsIgnCase(orgUnitId));

            if (orgUnit is null)
                return NotFound();

            // Clone the response as we must add stuff
            var responseObject = MockUtils.JsonClone<ApiOrgUnit>(orgUnit);
            
            // Populate parent node
            if (responseObject.ParentSapId is not null)
            {
                responseObject.Parent = LineOrgServiceMock.OrgUnits.Where(o => o.SapId.EqualsIgnCase(orgUnit.ParentSapId)).Select(o => MockUtils.JsonClone<ApiOrgUnitRef>(o)).FirstOrDefault();
            }

            if (query.ShouldExpand("children"))
            {
                responseObject.Children = LineOrgServiceMock.OrgUnits.Where(o => o.ParentSapId.EqualsIgnCase(orgUnit.SapId)).Select(o => MockUtils.JsonClone<ApiOrgUnitRef>(o) ).ToList();
            }


            return Ok(responseObject);

            //var departmentId = DepartmentId.FromFullPath(orgUnitId);
            //var parts = orgUnitId.Split(' ');
            //var orgUnit = new ApiOrgUnit
            //{
            //    BusinessArea = new ApiOrgUnitRef { Name = departmentId.BusinessArea, ShortName = departmentId.BusinessArea },
            //    Department = departmentId.LocalPath,
            //    FullDepartment = departmentId.FullPath,
            //    ParentPath = new ApiOrgUnit.OrgUnitParentPath
            //    {
            //        Level0 = (parts.Length > 0) ? parts[0] : null,
            //        Level1 = (parts.Length > 1) ? parts[1] : null,
            //        Level2 = (parts.Length > 2) ? parts[2] : null,
            //        Level3 = (parts.Length > 3) ? parts[3] : null,
            //        Level4 = (parts.Length > 4) ? parts[4] : null,
            //        Level5 = (parts.Length > 5) ? parts[5] : null,
            //        Level6 = (parts.Length > 6) ? parts[6] : null,
            //        Level7 = (parts.Length > 7) ? parts[7] : null,
            //        Level8 = (parts.Length > 8) ? parts[8] : null
            //    }
            //};
            //return Ok(orgUnit);
        }

    }

    internal static class MockUtils
    {
        public static T JsonClone<T>(object item)
        {
            var json = JsonConvert.SerializeObject(item);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
