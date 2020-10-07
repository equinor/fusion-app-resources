using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class DeleteContractPosition : TrackableRequest
    {
        public DeleteContractPosition(Guid projectId, Guid contractIdentifier, Guid positionId)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            OrgPositionId = positionId;
        }

        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }
        public Guid OrgPositionId { get; set; }


        public class Handler : AsyncRequestHandler<DeleteContractPosition>
        {
            private readonly IOrgApiClient client;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                client = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            protected override async Task Handle(DeleteContractPosition request, CancellationToken cancellationToken)
            {
                /*
                 * Special cases:
                 * - Deleted position is a REP position
                 * 
                 * */

                var positionResponse = await client.GetPositionV2Async(request.OrgProjectId, request.OrgContractId, request.OrgPositionId);

                if (positionResponse.IsSuccessStatusCode)
                {
                    await client.DeletePositionV2Async(positionResponse.Value, force: true);
                } 
                else
                {
                    switch (positionResponse.StatusCode)
                    {
                        case System.Net.HttpStatusCode.NotFound:
                            throw new InvalidOperationException("Position was not located in org chart.");
                    }

                    throw new InvalidOperationException("Unknown error when trying to comunicate with org api.");
                }
            }
        }
    }
}
