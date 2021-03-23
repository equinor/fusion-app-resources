using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class ResolveSubType : IRequest<string>
        {
            public ResolveSubType(Guid orgPositionId, Guid orgInstanceId)
            {
                OrgPositionId = orgPositionId;
                OrgInstanceId = orgInstanceId;
            }

            public Guid OrgPositionId { get; }
            public Guid OrgInstanceId { get; }

            public class Handler : IRequestHandler<ResolveSubType, string>
            {
                private readonly IProjectOrgResolver orgResolver;

                public Handler(IProjectOrgResolver orgResolver)
                {
                    this.orgResolver = orgResolver;
                }

                public async Task<string> Handle(ResolveSubType request, CancellationToken cancellationToken)
                {

                    var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId);
                    if (position is null) throw new InvalidOperationException("Could not resolve org position");

                    var basePosition = await orgResolver.ResolveBasePositionAsync(position.BasePosition.Id);
                    var bpSettings = basePosition?.GetTypedSettings();

                    if (bpSettings is not null && bpSettings.DirectAssignmentEnabled.GetValueOrDefault(false))
                        return AllocationDirectWorkflowV1.SUBTYPE;
                    else
                    {
                        // Check if joint venture
                        var instance = position.Instances.First(i => i.Id == request.OrgInstanceId);

                        if (string.Equals(instance?.Properties?.GetProperty<string>("type", "normal"), "jointVenture", StringComparison.OrdinalIgnoreCase))
                            return AllocationJointVentureWorkflowV1.SUBTYPE;
                        else
                            return AllocationNormalWorkflowV1.SUBTYPE;
                    }
                }
            }
        }
    }
}