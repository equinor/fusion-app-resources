using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Fusion.Resources.Domain.Commands
{
    public class UpdateContractPersonnelRequest : TrackableRequest<QueryPersonnelRequest>
    {
        public UpdateContractPersonnelRequest(Guid requestId)
        {
            RequestId = requestId;
        }


        public Guid RequestId { get; set; }

        public MonitorableProperty<PersonId> Person { get; set; } = new MonitorableProperty<PersonId>();


        public MonitorableProperty<Guid> BasePositionId { get; set; } = new MonitorableProperty<Guid>();
        public MonitorableProperty<string> PositionName { get; set; } = new MonitorableProperty<string>();
        public MonitorableProperty<DateTime> AppliesFrom { get; set; } = new MonitorableProperty<DateTime>();
        public MonitorableProperty<DateTime> AppliesTo { get; set; } = new MonitorableProperty<DateTime>();
        public MonitorableProperty<double> Workload { get; set; } = new MonitorableProperty<double>();
        public MonitorableProperty<Guid?> TaskOwnerPositionId { get; set; } = new MonitorableProperty<Guid?>();

        public MonitorableProperty<string> Description { get; set; } = new MonitorableProperty<string>();
        public MonitorableProperty<DbRequestState> State { get; set; } = new MonitorableProperty<DbRequestState>();


        public class Handler : IRequestHandler<UpdateContractPersonnelRequest, QueryPersonnelRequest>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
                this.mediator = mediator;
            }

            private DbContractorRequest dbRequest;
            private DbContractPersonnel? contractPersonnel;

            private async Task ValidateAsync(UpdateContractPersonnelRequest request)
            {
                dbRequest = await resourcesDb.ContractorRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                if (dbRequest is null)
                    throw new KeyNotFoundException($"Could not locate request with id {request.RequestId}");

                if (request.Person.HasBeenSet)
                {
                    var personnel = await profileService.ResolveExternalPersonnelAsync(request.Person.Value);

                    if (personnel is null)
                        throw new InvalidOperationException("The person does not exist in the personnel register. Include him/her before creating a request.");

                    contractPersonnel = await resourcesDb.ContractPersonnel.FirstOrDefaultAsync(p => p.ProjectId == dbRequest.ProjectId && p.ContractId == dbRequest.ContractId && p.PersonId == personnel.Id);

                    if (contractPersonnel is null)
                        throw new InvalidOperationException("The person was located as contractor personnel, but has not been added to this contract.");
                }
            }

            public async Task<QueryPersonnelRequest> Handle(UpdateContractPersonnelRequest request, CancellationToken cancellationToken)
            {
                await ValidateAsync(request);


                bool hasChanges = false;

                hasChanges |= UpdatePerson(request, dbRequest);
                hasChanges |= UpdatePosition(request, dbRequest);
                hasChanges |= UpdateRequest(request, dbRequest);
                
                if (hasChanges)
                {
                    dbRequest.Updated = DateTime.Now;
                    dbRequest.UpdatedBy = request.Editor.Person;

                    await resourcesDb.SaveChangesAsync();
                }

                var personnelRequest = await mediator.Send(new GetContractPersonnelRequest(request.RequestId));
                return personnelRequest;
            }

            private bool UpdatePerson(UpdateContractPersonnelRequest request, DbContractorRequest dbRequest)
            {
                return request.Person.IfSet(p => dbRequest.Person = contractPersonnel);
            }

            private bool UpdateRequest(UpdateContractPersonnelRequest request, DbContractorRequest dbRequest)
            {
                bool hasChanges = false;

                hasChanges |= request.State.IfSet(x => dbRequest.State = x);
                hasChanges |= request.Description.IfSet(x => dbRequest.Description = x);

                return hasChanges;
            }

            private bool UpdatePosition(UpdateContractPersonnelRequest request, DbContractorRequest dbRequest)
            {
                bool hasChanges = false;

                hasChanges |= request.AppliesFrom.IfSet(x => dbRequest.Position.AppliesFrom = x);
                hasChanges |= request.AppliesTo.IfSet(x => dbRequest.Position.AppliesTo = x);
                hasChanges |= request.BasePositionId.IfSet(x => dbRequest.Position.BasePositionId = x);
                hasChanges |= request.PositionName.IfSet(x => dbRequest.Position.Name = x);
                hasChanges |= request.Workload.IfSet(x => dbRequest.Position.Workload = x);
                hasChanges |= request.TaskOwnerPositionId.IfSet(x => dbRequest.Position.TaskOwner = new DbContractorRequest.PositionTaskOwner { PositionId = x });

                return hasChanges;
            }

   
        }
    }
}
