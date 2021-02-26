using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class JointVenture
        {

            public class Update : TrackableRequest<QueryResourceAllocationRequest>
            {
                public Update(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }
                public MonitorableProperty<string?> AssignedDepartment { get; private set; } = new();
                public MonitorableProperty<string?> Discipline { get; private set; } = new();
                public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; private set; } = new();
                public MonitorableProperty<string?> AdditionalNote { get; private set; } = new();
                public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; private set; } = new();
                public MonitorableProperty<bool> IsDraft { get; private set; } = new();

                public Update WithIsDraft(bool? isDraft)
                {
                    if (isDraft is not null) IsDraft = isDraft.GetValueOrDefault(true);
                    return this;
                }

                public Update WithAssignedDepartment(string? assignedDepartment)
                {
                    if (assignedDepartment is not null) AssignedDepartment = assignedDepartment;
                    return this;
                }

                public Update WithDiscipline(string? discipline)
                {
                    if (discipline is not null) Discipline = discipline;
                    return this;
                }
                public Update WithProposedPerson(Guid? proposedPersonAzureUniqueId)
                {
                    if (proposedPersonAzureUniqueId is not null)
                        ProposedPersonAzureUniqueId = proposedPersonAzureUniqueId;
                    return this;
                }

                public Update WithAdditionalNode(string? note)
                {
                    if (note is not null) AdditionalNote = note;
                    return this;
                }

                public Update WithProposedChanges(Dictionary<string, object>? changes)
                {
                    if (changes is not null) ProposedChanges = changes;
                    return this;
                }

                public class Validator : AbstractValidator<Update>
                {
                    public Validator(IProfileService profileService)
                    {
                        RuleFor(x => x.ProposedChanges.Value).BeValidProposedChanges().When(x => x.ProposedChanges.HasBeenSet && x.ProposedChanges.Value != null);
                        RuleFor(x => x.ProposedPersonAzureUniqueId).MustAsync(async (id, cancel) =>
                            {
                                var profile = await profileService.EnsurePersonAsync(new PersonId(id.Value!.Value));
                                return profile != null;

                            }).WithMessage("Profile must exist in profile service")
                            .When(x => x.ProposedPersonAzureUniqueId.Value != null);

                    }
                }

                public class Handler : IRequestHandler<Update, QueryResourceAllocationRequest
                >
                {
                    private readonly ResourcesDbContext db;
                    private readonly IMediator mediator;
                    private readonly IProfileService profileService;
                    
                    public Handler(IProfileService profileService, ResourcesDbContext db, IMediator mediator)
                    {
                        this.profileService = profileService;
                        this.db = db;
                        this.mediator = mediator;
                    }

                    public async Task<QueryResourceAllocationRequest> Handle(Update request, CancellationToken cancellationToken)
                    {
                        var dbEntity = await db.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                        if (dbEntity is null)
                            throw new RequestNotFoundError(request.RequestId);
                        
                        var item = await PersistChangesAsync(request, dbEntity);

                        var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(item.Id));
                        return requestItem!;
                    }

                    private async Task<DbResourceAllocationRequest> PersistChangesAsync(Update request, DbResourceAllocationRequest dbItem)
                    {
                        bool modified = false;
                        var updated = DateTimeOffset.UtcNow;

                        if (request.AssignedDepartment.HasBeenSet)
                        {
                            dbItem.AssignedDepartment = request.AssignedDepartment.Value;
                            modified = true;
                        }

                        if (request.Discipline.HasBeenSet)
                        {
                            dbItem.Discipline = request.Discipline.Value;
                            modified = true;
                        }

                        if (request.ProposedPersonAzureUniqueId.HasBeenSet)
                        {
                            if (request.ProposedPersonAzureUniqueId.Value != null)
                                dbItem.ProposedPerson = await profileService.EnsurePersonAsync(new PersonId(request.ProposedPersonAzureUniqueId.Value.Value));
                            else
                                dbItem.ProposedPerson = null;
                            modified = true;
                        }

                        if (request.AdditionalNote.HasBeenSet)
                        {
                            dbItem.AdditionalNote = request.AdditionalNote.Value;
                            modified = true;
                        }

                        if (request.ProposedChanges.HasBeenSet)
                        {
                            dbItem.ProposedChanges = request.ProposedChanges.Value.SerializeToString();
                            modified = true;
                        }

                        if (request.IsDraft.HasBeenSet)
                        {
                            dbItem.IsDraft = request.IsDraft.Value;
                            modified = true;
                        }

                        /*
                        {
                            dbItem.ProposedPersonWasNotified =  // Should be set/reset during update when/if when notifications are enabled
                            modified = true;
                        }
                        */

                        if (modified)
                        {
                            dbItem.Updated = updated;
                            dbItem.UpdatedBy = request.Editor.Person;
                            dbItem.LastActivity = updated;

                            await db.SaveChangesAsync();
                        }

                        return dbItem;
                    }
                }
            }
        }
    }
}