using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using Fusion.Integration.Profile.ApiClient;
using System.Linq;
using Fusion.AspNetCore.OData;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fusion.Testing.Mocks.ProfileService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    public class PersonsController : ControllerBase
    {
        [MapToApiVersion("3.0")]
        [HttpGet("/persons/{identifier}")]
        public ActionResult<ApiPersonProfileV3> ResolveProfileV3(string identifier, ODataExpandParams expandParams)
        {
            var profile = default(ApiPersonProfileV3);

            if (Guid.TryParse(identifier, out Guid uniqueId))
                profile = PeopleServiceMock.profiles.FirstOrDefault(p => p.AzureUniqueId == uniqueId);
            else
                profile = PeopleServiceMock.profiles.FirstOrDefault(p => p.Mail != null && p.Mail.ToLower() == identifier.ToLower());

            if (profile == null)
                return NotFound();

            // Must take a copy, as we don't want to change the "database"
            var copy = JsonConvert.DeserializeObject<ApiPersonProfileV3>(JsonConvert.SerializeObject(profile));

            copy.Roles = copy.Roles ?? new List<ApiPersonRoleV3>();
            copy.Positions = copy.Positions ?? new List<ApiPersonPositionV3>();
            copy.Contracts = copy.Contracts ?? new List<ApiPersonContractV3>();

            if (!expandParams.ShouldExpand("roles"))
                copy.Roles = null;
                

            if (!expandParams.ShouldExpand("positions"))
                copy.Positions = null;

            if (!expandParams.ShouldExpand("contracts"))
                copy.Contracts = null;

            return copy;
        }
    }
}
