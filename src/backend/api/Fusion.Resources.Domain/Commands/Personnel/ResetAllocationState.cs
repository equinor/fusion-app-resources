using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{
    public class ResetAllocationState : TrackableRequest
    {
        public ResetAllocationState(Guid orgProjectId, Guid orgPositionId, Guid orgInstanceId)
        {
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            OrgInstanceId = orgInstanceId;
        }

        public Guid OrgProjectId { get; }
        public Guid OrgPositionId { get; }
        public Guid OrgInstanceId { get; }

        public class Handler : AsyncRequestHandler<ResetAllocationState>
        {
            private readonly ILogger<Handler> logger;
            private readonly ResourcesDbContext dbContext;
            private readonly IOrgApiClientFactory orgApiClientFactory;

            public Handler(ILogger<Handler> logger, ResourcesDbContext dbContext, IOrgApiClientFactory orgApiClientFactory)
            {
                this.logger = logger;
                this.dbContext = dbContext;
                this.orgApiClientFactory = orgApiClientFactory;
            }

            protected override async Task Handle(ResetAllocationState request, CancellationToken cancellationToken)
            {
                var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);

                var resetStateUrl = $"/projects/{request.OrgProjectId}/positions/{request.OrgPositionId}/instances/{request.OrgInstanceId}/allocation-state/reset";
                var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, resetStateUrl));

                if (!resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync();
                    logger.LogError("Received error from org api {StatusCode}: " + content, resp.StatusCode);

                    throw new IntegrationError("Org api failed when trying to reset allocation state", new OrgApiError(resp, content));
                }
            }
        }
    }
}
