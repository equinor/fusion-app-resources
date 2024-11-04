using Microsoft.AspNetCore.Mvc;
using System;
using Fusion.Integration.Profile.ApiClient;
using System.Linq;
using Fusion.AspNetCore.OData;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;


namespace Fusion.Testing.Mocks.ProfileService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
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
        [MapToApiVersion("3.0")]
        [HttpPost("/persons/ensure")]
        public ActionResult<ApiEnsuredProfileV2> ResolveProfileV3(ProfilesRequest request)
        {
            var results = new List<ApiEnsuredProfileV2>();

            foreach (var id in request.PersonIdentifiers)
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
        public ActionResult Search(PeopleSearchRequest peopleSearchRequest)
        {
            var props = typeof(ApiPersonProfileV3).GetProperties();

            // NOTE: 
            // 
            // Note sure this "mock" does what we want.. But do not know that it is that important to have an exact filter here as we are not verifying results.
            //

            var azureUniqueId = GetAzureUniqueIdFromFilterQuery(peopleSearchRequest);

            return Ok(new
                      {
                          Results = !string.IsNullOrWhiteSpace(azureUniqueId) 
                              ? PeopleServiceMock.profiles.Where(x => x.AzureUniqueId.ToString() == azureUniqueId).Select(PeopleSelector(props))
                              : PeopleServiceMock.profiles.Select(PeopleSelector(props))
                      });
        }

        private static Func<ApiPersonProfileV3, object> PeopleSelector(PropertyInfo[] props)
        {
            return profile =>
                   {
                       var doc = new Dictionary<string, object>();

                       foreach (var prop in props) doc.Add(prop.Name, prop.GetValue(profile));

                       doc["ManagerAzureId"] = PeopleServiceMock.profiles.FirstOrDefault(m => m.IsResourceOwner && m.FullDepartment == profile.FullDepartment)?.AzureUniqueId;
                       doc["IsExpired"] = false;

                       return new
                              {
                                  Document = doc
                              };
                   };
        }

        private static string GetAzureUniqueIdFromFilterQuery(PeopleSearchRequest peopleSearchRequest)
        {
            if (string.IsNullOrWhiteSpace(peopleSearchRequest.Filter) || !peopleSearchRequest.Filter.Contains("azureUniqueId")) return null;
            
            var edmModel = new EdmModel();
            var persons = new EdmEntityType("AdPerson", "Person");
            persons.AddStructuralProperty("azureUniqueId", EdmPrimitiveTypeKind.String);
            persons.AddStructuralProperty("managerAzureId", EdmPrimitiveTypeKind.String);
            persons.AddStructuralProperty("fullDepartment", EdmPrimitiveTypeKind.String);
            persons.AddStructuralProperty("isExpired", EdmPrimitiveTypeKind.Boolean);
            persons.AddStructuralProperty("isResourceOwner", EdmPrimitiveTypeKind.Boolean);
            edmModel.AddElement(persons);
            var filterClause = ODataUriParser.ParseFilter(peopleSearchRequest.Filter, edmModel, persons);
            var operatorNode = filterClause.Expression as BinaryOperatorNode;
            var propertyName = (operatorNode?.Left as SingleValuePropertyAccessNode)?.Property.Name;
            
            if (propertyName is not "azureUniqueId") return null;

            var value = (operatorNode.Right as ConstantNode)?.Value;

            return value as string;
        }

        public class ProfilesRequest
        {
            public List<string> PersonIdentifiers { get; set; }
        }
    }

    public class PeopleSearchRequest
    {
        public string Filter { get; set; }
        public int Top { get; set; }
    }
}
