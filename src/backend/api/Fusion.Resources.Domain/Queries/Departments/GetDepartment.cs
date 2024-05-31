using Fusion.Integration;
using Fusion.Integration.Diagnostics;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Database;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartment : IRequest<QueryDepartment?>
    {
        private bool shouldExpandDelegatedResourceOwners;

        public string DepartmentId { get; }

        public GetDepartment(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public GetDepartment ExpandDelegatedResourceOwners()
        {
            shouldExpandDelegatedResourceOwners = true;
            return this;
        }

        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartment, QueryDepartment?>
        {
        private readonly IHttpClientFactory _httpClientFactory;
            private readonly IFusionLogger<GetDepartment> logger;
            public Handler(IHttpClientFactory httpClientFactory, ResourcesDbContext  db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver, IFusionLogger<GetDepartment> logger)
                : base(db, lineOrgResolver, profileResolver) {
                this._httpClientFactory = httpClientFactory;
                this.logger = logger;
            }

            public async Task<QueryDepartment?> Handle(GetDepartment request, CancellationToken cancellationToken)
            {
                var client = _httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg());
                var departmentId = Integration.LineOrg.DepartmentId.FromFullPath(request.DepartmentId);

                var uri = $"/org-units/{departmentId}?$expand=parentPath";
                var httpResponse = await client.GetAsync(uri);
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (!httpResponse.IsSuccessStatusCode)
                {
                    var message = $"Read department info from line org failed with status {httpResponse.StatusCode}.";
                    logger.LogCritical(message);

                    await LineOrgIntegrationError.ThrowFromResponse(message, httpResponse);
                }
                var lineOrgDpt = JsonConvert.DeserializeObject<ApiDepartment>(
                    await httpResponse.Content.ReadAsStringAsync()
                );

                if (lineOrgDpt is null) return null;

                var sector = new DepartmentPath(lineOrgDpt.FullName).Parent();
                var result = new QueryDepartment(lineOrgDpt.FullName, sector);

                if (request.shouldExpandDelegatedResourceOwners)
                    await ExpandDelegatedResourceOwner(result, cancellationToken);

                if (lineOrgDpt?.Manager?.AzureUniqueId is not null)
                {
                    result.LineOrgResponsible = await profileResolver.ResolvePersonBasicProfileAsync(new Integration.Profile.PersonIdentifier(lineOrgDpt.Manager.AzureUniqueId));
                }

                return result;
            }
        }
    }
}