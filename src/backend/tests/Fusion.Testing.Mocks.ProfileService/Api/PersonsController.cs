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

        [MapToApiVersion("2.0")]
        [HttpPost("/persons/ensure")]
        public ActionResult<ApiEnsuredProfileV2> ResolveProfileV3(ProfilesRequest request)
        {
            var results = new List<ApiEnsuredProfileV2>();
            
            foreach(var id in request.PersonIdentifiers)
            {
                var result = new ApiEnsuredProfileV2
                {
                    Identifier = id,
                    StatusCode = 404,
                    Success = false
                };

                var profile = PeopleServiceMock.profiles
                    .FirstOrDefault(p => p.AzureUniqueId.ToString() == id || p.Mail == id);

                if (profile is not null)
                {
                    var copy = JsonConvert.DeserializeObject<ApiPersonProfileV2>(JsonConvert.SerializeObject(profile));

                    result.StatusCode = 200;
                    result.Success = true;
                    result.Person = copy;
                }

                results.Add(result);
            }

            return Ok(results);
        }

        [HttpPost("/search/persons/query")]
        public ActionResult Search()
        {
            var props = typeof(ApiPersonProfileV3).GetProperties();
            return Ok(new
            {
                Results = PeopleServiceMock.profiles
                .Select(x => {
                    var doc = new Dictionary<string, object>();
                    
                    foreach(var prop in props) doc.Add(prop.Name, prop.GetValue(x));

                    doc["ManagerAzureId"] = Guid.NewGuid();
                    doc["IsExpired"] = false;
                    doc["Positions"] = new List<ApiPersonPositionV3>();

                    return new
                    {
                        Document = doc
                    };
                })
            });
        }
        public class ProfilesRequest
        {
            public List<string> PersonIdentifiers { get; set; }
        }
    }
}
