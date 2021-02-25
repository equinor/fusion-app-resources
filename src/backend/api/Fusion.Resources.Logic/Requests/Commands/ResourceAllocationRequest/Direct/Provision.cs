using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.Integration.Org;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Direct
        {
            public class Provision : TrackableRequest
            {
                public Provision(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }

                public class Validator : AbstractValidator<Provision>
                {
                    public Validator(ResourcesDbContext db, IProjectOrgResolver projectOrgResolver)
                    {
                        RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        {
                            return await db.ResourceAllocationRequests.AnyAsync(y =>
                                y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Direct);
                        }).WithMessage($"Request must exist.");

                        // Based on request, does the org position instance exist ?
                        RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        {
                            var request = await db.ResourceAllocationRequests.FirstOrDefaultAsync(y => y.Id == id);

                            if (request.OrgPositionId == null)
                                return false;

                            var position = await projectOrgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
                            var instance =
                                position?.Instances.FirstOrDefault(x => x.Id == request.OrgPositionInstance.Id);
                            return instance != null;

                        }).WithMessage($"Org position instance must exist.");
                    }
                }

                public class Handler : AsyncRequestHandler<Provision>
                {
                    private readonly ResourcesDbContext resourcesDb;
                    private readonly IMediator mediator;

                    public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
                    {
                        this.resourcesDb = resourcesDb;
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Provision request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);


                        if (dbRequest is null)
                            throw new RequestNotFoundError(request.RequestId);


                        await UpdatePositionAsync(dbRequest);

                        await resourcesDb.SaveChangesAsync();
                    }

                    private static Dictionary<string, string> TryConvertToDictionary(string? objectString)
                    {
                        if (objectString is null)
                            return new Dictionary<string, string>();

                        try
                        {
                            var objDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(objectString);

                            var ret = new Dictionary<string, string>();
                            foreach (var o in objDict)
                            {
                                
                                ret.Add(o.Key, o.Value.GetString()!);
                            }
                            return ret;
                        }
                        catch(Exception ex)
                        {
                            var x = ex.Message;
                            return new Dictionary<string, string>();
                        }
                    }


                    private async Task UpdatePositionAsync(DbResourceAllocationRequest dbRequest)
                    {
                        if (dbRequest.OrgPositionId == null)
                            throw new InvalidOperationException(
                                "Cannot provision change request when original position id is empty.");


                        if (dbRequest.ProposedChanges != null)
                        {
                            var instanceChanges = TryConvertToDictionary(dbRequest.ProposedChanges).DictionaryToObject<ApiPositionInstanceV2>();

                            var updatePositionCommand = new UpdatePositionInstance(dbRequest.Project.OrgProjectId, instanceChanges);

                            dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                            dbRequest.LastActivity = DateTime.UtcNow;

                            try
                            {
                                var position = await mediator.Send(updatePositionCommand);
                                dbRequest.ProvisioningStatus.State =
                                    DbResourceAllocationRequest.DbProvisionState.Provisioned;
                                dbRequest.ProvisioningStatus.PositionId = position.Id;

                                dbRequest.ProvisioningStatus.ErrorMessage = null;
                                dbRequest.ProvisioningStatus.ErrorPayload = null;
                            }
                            catch (OrgApiError apiError)
                            {
                                dbRequest.ProvisioningStatus.ErrorMessage =
                                    $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                                dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                                dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                            }
                        }
                    }
                }
            }
        }
    }
}
