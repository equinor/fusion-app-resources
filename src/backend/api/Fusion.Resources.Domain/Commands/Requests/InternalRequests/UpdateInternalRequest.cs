using Fusion.Resources.Database;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;

namespace Fusion.Resources.Domain.Commands
{

    public class UpdateInternalRequest : TrackableRequest<QueryResourceAllocationRequest>
    {
        public UpdateInternalRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public MonitorableProperty<string?> AssignedDepartment { get; set; } = new();
        public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; set; } = new();
        public MonitorableProperty<string?> AdditionalNote { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> Properties { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; set; } = new();

        public MonitorableProperty<DateTime?> ProposalChangeFrom { get; set; } = new();
        public MonitorableProperty<DateTime?> ProposalChangeTo { get; set; } = new();
        public MonitorableProperty<ProposalChangeScope> ProposalScope { get; set; } = new();

        // Placeholder, not used currently
        public MonitorableProperty<string?> ProposalChangeType { get; set; } = new();

        public MonitorableProperty<List<PersonId>> Candidates { get; set; } = new();

        public class Handler : IRequestHandler<UpdateInternalRequest, QueryResourceAllocationRequest>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IProfileService profileService, IMediator mediator)
            {
                this.db = db;
                this.profileService = profileService;
                this.mediator = mediator;
            }

            public async Task<QueryResourceAllocationRequest> Handle(UpdateInternalRequest request, CancellationToken cancellationToken)
            {
                var dbRequest = await db.ResourceAllocationRequests.FirstAsync(r => r.Id == request.RequestId);

                bool modified = false;

                modified |= request.AssignedDepartment.IfSet(dep => dbRequest.AssignedDepartment = dep);
                modified |= request.AdditionalNote.IfSet(note => dbRequest.AdditionalNote = note);
                modified |= request.AdditionalNote.IfSet(note => dbRequest.AdditionalNote = note);
                modified |= request.ProposedChanges.IfSet(changes => dbRequest.ProposedChanges = changes.SerializeToStringOrDefault());
                modified |= await request.Properties.IfSetAsync(async properties =>
                {
                    if (properties is not null)
                    {
                        var resolvedProperties = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                        var exsistingProps = JsonConvert.DeserializeObject<Dictionary<string, object>>(resolvedProperties.PropertiesJson) ?? new Dictionary<string, object>();

                        foreach (var property in properties)
                        {

                            if (property.Value == null && string.IsNullOrEmpty(property.Value?.ToString()))
                            {
                                exsistingProps.Remove(property.Key);
                            }
                            else
                            {
                                exsistingProps[property.Key] = property.Value;
                            }
                        }
                        dbRequest.Properties = exsistingProps.SerializeToStringOrDefault();
                    }




                });
                modified |= await request.ProposedPersonAzureUniqueId.IfSetAsync(async personId =>
                    {
                        if (personId is not null)
                        {
                            var resolvedPerson = await profileService.EnsurePersonAsync(new PersonId(personId.Value));
                            dbRequest.ProposePerson(resolvedPerson!);
                        }
                        else
                        {
                            dbRequest.ProposedPerson.Clear();
                        }
                    });
                modified |= await request.Candidates.IfSetAsync(async candidates =>
                {
                    dbRequest.Candidates.Clear();
                    foreach (var personId in candidates)
                    {
                        var resolvedPerson = await profileService.EnsurePersonAsync(personId);
                        if (resolvedPerson is null) throw new Exception();

                        dbRequest.Candidates.Add(resolvedPerson);
                    }

                    if (dbRequest.Candidates.Count == 1 && !dbRequest.ProposedPerson.HasBeenProposed)
                    {
                        dbRequest.ProposePerson(dbRequest.Candidates.Single());
                    }
                });
                modified |= request.ProposalChangeFrom.IfSet(dt => dbRequest.ProposalParameters.ChangeFrom = dt);
                modified |= request.ProposalChangeTo.IfSet(dt => dbRequest.ProposalParameters.ChangeTo = dt);
                modified |= request.ProposalChangeType.IfSet(dt => dbRequest.ProposalParameters.ChangeType = dt);
                modified |= request.ProposalScope.IfSet(dt => dbRequest.ProposalParameters.Scope = dt.MapToDatabase());

                if (modified)
                {
                    dbRequest.Updated = DateTimeOffset.UtcNow;
                    dbRequest.UpdatedBy = request.Editor.Person;
                    dbRequest.LastActivity = dbRequest.Updated.Value;

                    var modifiedProperties = db.Entry(dbRequest).Properties.Where(x => x.IsModified).ToList();

                    await db.SaveChangesAsync(cancellationToken);

                    await mediator.Publish(new InternalRequestUpdated(dbRequest.Id, modifiedProperties));
                }

                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                return requestItem!;
            }
        }
    }
}
