using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update a simple contract posision.
    /// This will update a single instance position. If the position is detected to have multi instances etc, an error will be thrown.
    /// </summary>
    public class UpdateContractPosition : TrackableRequest<ApiClients.Org.ApiPositionV2>
    {
        public UpdateContractPosition(ApiPositionV2 position)
        {
            if (position.Project == null || position.Project.ProjectId == Guid.Empty)
                throw new ArgumentException("Position property must be defined and contain non-empty id.");

            if (position.Contract == null || position.Contract.Id == Guid.Empty)
                throw new ArgumentException("Contract property must be defined and contain non-empty id.");

            if (position.BasePosition == null || position.BasePosition.Id == Guid.Empty)
                throw new ArgumentException("BasePosition property must be defined and contain non-empty id.");


            OrgProjectId = position.Project.ProjectId;
            OrgContractId = position.Contract.Id;
            PositionId = position.Id;

            BasePositionId = position.BasePosition.Id;
            PositionName = position.Name;
            ExternalId = position.ExternalId;

            var instance = position.Instances.FirstOrDefault();
            if (instance != null)
            {
                AppliesFrom = instance.AppliesFrom;
                AppliesTo = instance.AppliesTo;
                Workload = instance.Workload ?? 100;
                Obs = instance.Obs;
                AssignedPerson = instance.AssignedPerson;
            }

        }

        public UpdateContractPosition(Guid projectId, Guid contractIdentifier, Guid positionId)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            PositionId = positionId;
        }

        public Guid OrgProjectId { get; }
        public Guid OrgContractId { get; }
        public Guid PositionId { get; }

        public Guid BasePositionId { get; set; }
        public string? PositionName { get; set; }
        public string? ExternalId { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double? Workload { get; set; }

        public PersonId? AssignedPerson { get; set; }
        public string? Obs { get; set; }

        public Guid? ParentPositionId { get; set; }


        public class Handler : IRequestHandler<UpdateContractPosition, ApiClients.Org.ApiPositionV2>
        {
            private readonly IOrgApiClient orgClient;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiPositionV2> Handle(UpdateContractPosition request, CancellationToken cancellationToken)
            {
                var getresp = await orgClient.GetPositionV2Async(request.OrgProjectId, request.OrgContractId, request.PositionId);

                if (!getresp.IsSuccessStatusCode)
                    throw new OrgApiError(getresp.Response, getresp.Content);

                var position = getresp.Value;

                if (position.Instances.Count > 1)
                    throw GenerateMultiInstanceError(position);

                position.BasePosition = new ApiPositionBasePositionV2 { Id = request.BasePositionId };
                position.Name = request.PositionName;
                position.ExternalId = request.ExternalId;

                var instance = position.Instances.FirstOrDefault();
                if (instance == null)
                {
                    instance = new ApiPositionInstanceV2();
                    position.Instances.Add(instance);
                }

                instance.AppliesFrom = request.AppliesFrom;
                instance.AppliesTo = request.AppliesTo;
                instance.Obs = request.Obs;
                instance.Workload = request.Workload;
                instance.AssignedPerson = request.AssignedPerson;
                instance.ParentPositionId = request.ParentPositionId;


                var resp = await orgClient.PutPositionAsync(position);

                if (resp.IsSuccessStatusCode)
                    return resp.Value;

                throw new OrgApiError(resp.Response, resp.Content);
            }

            private InvalidOperationException GenerateMultiInstanceError(ApiPositionV2 position)
            {
                var instances = string.Join(", ", position.Instances.OrderBy(i => i.AppliesFrom).Select(i => $"{i.AppliesFrom.ToString("yyyy-MM-dd")} -> {i.AppliesTo.ToString("yyyy-MM-dd")}"));
                return new InvalidOperationException($"Cannot update a position with multiple instances. Detected {position.Instances.Count} instances on {position.Name}, {instances}");
            }
        }
    }
}
