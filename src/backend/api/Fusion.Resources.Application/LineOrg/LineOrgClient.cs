using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.LineOrg.Cache;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public class LineOrgClient : ILineOrgClient 
    {
        private readonly ILogger<LineOrgClient> logger;
        private readonly ILineOrgCache cache;
        private HttpClient client;

        public LineOrgClient(ILogger<LineOrgClient> logger, IHttpClientFactory httpClientFactory, ILineOrgCache cache)
        {
            client = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg());
            this.logger = logger;
            this.cache = cache;
        }

        public async Task<ApiOrgUnit?> ResolveOrgUnitAsync(string identifier, Action<OrgUnitExpand>? expand)
        {
            var expandOptions = new OrgUnitExpand();
            expand?.Invoke(expandOptions);

            var departmentId = Regex.IsMatch(identifier, @"\d+") ? Integration.LineOrg.DepartmentId.FromSapId(identifier)
                : Integration.LineOrg.DepartmentId.FromFullPath(identifier);

            if (cache.TryGetOrgUnit(departmentId, out var orgUnit))
            {
                bool isCacheValid = orgUnit is not null && orgUnit.Parent is not null;

                if (expandOptions.ShouldExpandChildren == true && orgUnit?.Children is null)
                {
                    isCacheValid = false;
                }

                if (expandOptions.ShouldExpandManagement == true && orgUnit?.Management is null)
                {
                    isCacheValid = false;
                }

                if (expandOptions.ShouldExpandParentPath == true && orgUnit?.ParentPath is null)
                {
                    isCacheValid = false;
                }

                if (isCacheValid && orgUnit is not null)
                    return orgUnit;
            }

            // Load new
            var resp = await client.GetAsync($"/org-units/{identifier}?$expand={expandOptions}&api-version=1.0");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                var message = $"Error with request '{resp.RequestMessage?.RequestUri}'. Responded with {resp.StatusCode}.";
                logger.LogCritical(message);

                await LineOrgIntegrationError.ThrowFromResponse(message, resp);
            }

            // Normal response
            var data = await resp.Content.ReadAsStringAsync();
            orgUnit = JsonConvert.DeserializeObject<ApiOrgUnit>(data);

            if (orgUnit != null)
            {
                cache.Set(orgUnit);
            }

            return orgUnit;
        }

        public class OrgUnitExpand
        {
            public List<string> ExpandList { get; set; } = new();
            
            public bool? ShouldExpandChildren { get; set; }
            public bool? ShouldExpandManagement { get; set; } = true;
            public bool? ShouldExpandParentPath{ get; set; } = true;

            public OrgUnitExpand() { }

            public OrgUnitExpand ExpandChildren() {
                ExpandList.Add("children");
                ShouldExpandChildren = true;

                return this;
            }

            public OrgUnitExpand ExpandManagement()
            {
                ExpandList.Add("management");
                ShouldExpandManagement = true;

                return this;
            }

            public OrgUnitExpand ExpandParentPath()
            {
                ExpandList.Add("parentPath");
                ShouldExpandParentPath = true;

                return this;
            }

            public override string ToString()
            {
                return string.Join(",", ExpandList);
            }
        }
    }

}
