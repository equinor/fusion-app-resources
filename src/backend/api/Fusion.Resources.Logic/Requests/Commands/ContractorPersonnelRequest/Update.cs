using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {
        public class Update : TrackableRequest<QueryPersonnelRequest>
        {
            public Update(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }

            public MonitorableProperty<PersonId> Person { get; set; } = new MonitorableProperty<PersonId>();

            public MonitorableProperty<string?> Description { get; set; } = new MonitorableProperty<string?>();
            public MonitorableProperty<PositionInfo> Position { get; set; } = new MonitorableProperty<PositionInfo>();
            public MonitorableProperty<Guid?> PositionTaskOwner { get; set; } = new MonitorableProperty<Guid?>();


            public Update SetPerson(PersonId person)
            {
                Person = person;
                return this;
            }
            public Update SetPosition(Guid basePositionId, string name, DateTime from, DateTime to, double workload, string? obs)
            {
                var position = new PositionInfo()
                {
                    BasePositionId = basePositionId,
                    PositionName = name,
                    AppliesFrom = from,
                    AppliesTo = to,
                    Workload = workload,
                    Obs = obs ?? string.Empty
                };

                Position = position;

                return this;
            }
            public Update SetTaskOwner(Guid? positionId)
            {
                PositionTaskOwner = positionId;

                return this;
            }
            public Update SetDescription(string? description)
            {
                Description = description;
                return this;
            }


            public class Handler : IRequestHandler<Update, QueryPersonnelRequest>
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

                DbContractorRequest dbRequest = null!;
                DbContractPersonnel contractPersonnel = null!;

                public async Task ValidateAsync(Update request)
                {

                    dbRequest = await resourcesDb.ContractorRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                    if (dbRequest is null)
                        throw new RequestNotFoundError(request.RequestId);

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

                public async Task<QueryPersonnelRequest> Handle(Update request, CancellationToken cancellationToken)
                {
                    // Validate references.
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

                private bool UpdatePerson(Update request, DbContractorRequest dbRequest)
                {
                    return request.Person.IfSet(p => dbRequest.Person = contractPersonnel);
                }

                private bool UpdateRequest(Update request, DbContractorRequest dbRequest)
                {
                    bool hasChanges = false;

                    hasChanges |= request.Description.IfSet(x => dbRequest.Description = x ?? string.Empty);

                    return hasChanges;
                }

                private bool UpdatePosition(Update request, DbContractorRequest dbRequest)
                {
                    bool hasChanges = false;

                    if (request.Position.HasBeenSet)
                    {
                        hasChanges = true;
                        var position = request.Position.Value;

                        dbRequest.Position.AppliesFrom = position.AppliesFrom;
                        dbRequest.Position.AppliesTo = position.AppliesTo;
                        dbRequest.Position.BasePositionId = position.BasePositionId;
                        dbRequest.Position.Name = position.PositionName;
                        dbRequest.Position.Workload = position.Workload;
                        dbRequest.Position.Obs = position.Obs;
                    }

                    hasChanges |= request.PositionTaskOwner.IfSet(x => dbRequest.Position.TaskOwner = new DbContractorRequest.PositionTaskOwner { PositionId = x });

                    return hasChanges;
                }

            }
        }
    }
}
