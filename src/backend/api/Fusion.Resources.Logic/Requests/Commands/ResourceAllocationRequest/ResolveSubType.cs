using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using System;
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


                    if (bpSettings is not null && bpSettings.DirectAssignmentEnabled.GetValueOrDefault(false) )
                        return AllocationDirectWorkflowV1.SUBTYPE;
                    else
                    {
                        if (string.Equals(position?.Properties?.GetProperty<string>("resourceType", "normal"), "jointVenture", StringComparison.OrdinalIgnoreCase))
                            return AllocationJointVentureWorkflowV1.SUBTYPE;
                        else if (string.Equals(position?.Properties?.GetProperty<string>("resourceType", "normal"), "enterprise", StringComparison.OrdinalIgnoreCase))
                            return AllocationEnterpriseWorkflowV1.SUBTYPE;
                        else
                        {
                            // Check if the base position requires direct request.
                            if (basePosition?.RequiresDirectRequest() == true)
                                return AllocationDirectWorkflowV1.SUBTYPE;

                            return AllocationNormalWorkflowV1.SUBTYPE;
                        }
                    }
                }
            }
        }
    }
}