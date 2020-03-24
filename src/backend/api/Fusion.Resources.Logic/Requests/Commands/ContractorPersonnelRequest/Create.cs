using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {

        public class Create : TrackableRequest<QueryPersonnelRequest>
        {
            public Create(Guid orgProjectId, Guid orgContractId, PersonId person)
            {
                OrgProjectId = orgProjectId;
                OrgContractId = orgContractId;
                Person = person;
            }


            public Guid OrgContractId { get; }
            public Guid OrgProjectId { get; }
            public PersonId Person { get; }

            public string? Description { get; set; }
            public PositionInfo Position { get; } = new PositionInfo();
            public Guid? PositionTaskOwner { get; set; }
            public Guid? OriginalPositionId { get; set; }



            public Create WithOriginalPosition(Guid? originalPositionId)
            {
                OriginalPositionId = originalPositionId;
                return this;
            }

            public Create WithPosition(Guid basePositionId, string name, DateTime from, DateTime to, double workload, string? obs)
            {
                Position.BasePositionId = basePositionId;
                Position.PositionName = name;
                Position.AppliesFrom = from;
                Position.AppliesTo = to;
                Position.Workload = workload;
                Position.Obs = obs ?? string.Empty;

                return this;
            }
            public Create WithTaskOwner(Guid? positionId)
            {
                PositionTaskOwner = positionId;

                return this;
            }
            public Create WithDescription(string? description)
            {
                Description = description;
                return this;
            }

            public class Handler : IRequestHandler<Create, QueryPersonnelRequest>
            {
                private readonly ResourcesDbContext resourcesDb;
                private readonly IProfileService profileService;
                private readonly IMediator mediator;
                private readonly IProjectOrgResolver orgResolver;

                public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IMediator mediator, IProjectOrgResolver orgResolver)
                {
                    this.resourcesDb = resourcesDb;
                    this.profileService = profileService;
                    this.mediator = mediator;
                    this.orgResolver = orgResolver;
                }

                DbProject project = null!;
                DbContract contract = null!;
                DbContractPersonnel contractPersonnel = null!;

                public async Task ValidateAsync(Create request)
                {
                    project = await resourcesDb.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == request.OrgProjectId);
                    contract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == request.OrgContractId);

                    if (project is null)
                        throw new InvalidOperationException("Could not locate the project, does it have any contracts allocated?");

                    if (contract is null)
                        throw new InvalidOperationException($"Cannot create personnel to unallocated contracts. Could not locate any contracts with id {request.OrgContractId}.");

                    var personnel = await profileService.ResolveExternalPersonnelAsync(request.Person);

                    if (personnel is null)
                        throw new InvalidOperationException("The person does not exist in the personnel register. Include him/her before creating a request.");

                    contractPersonnel = await resourcesDb.ContractPersonnel.FirstOrDefaultAsync(p => p.ProjectId == project.Id && p.ContractId == contract.Id && p.PersonId == personnel.Id);

                    if (contractPersonnel is null)
                        throw new InvalidOperationException("The person was located as contractor personnel, but has not been added to this contract.");

                    await ValidateOriginalPositionAsync(request);
                    await ValidateIsOnlyActiveChangeRequestAsync(request);
                }

                public async Task<QueryPersonnelRequest> Handle(Create request, CancellationToken cancellationToken)
                {
                    // Validate references.
                    await ValidateAsync(request);

                    var newRequest = await PersistChangesAsync(request);

                    // Start the workflow
                    await mediator.Send(new Initialize(newRequest.Id));

                    var personnelRequest = await mediator.Send(new GetContractPersonnelRequest(newRequest.Id));
                    return personnelRequest;
                }

                /// <summary>
                /// Persist the new request to the database 
                /// </summary>
                private async Task<DbContractorRequest> PersistChangesAsync(Create request)
                {
                    var category = request.OriginalPositionId.HasValue ? DbRequestCategory.ChangeRequest : DbRequestCategory.NewRequest;

                    var newRequest = new DbContractorRequest()
                    {
                        Id = Guid.NewGuid(),
                        Contract = contract,
                        Project = project,
                        State = DbRequestState.Created,
                        Person = contractPersonnel,
                        Category = category,
                        OriginalPositionId = request.OriginalPositionId,
                        Position = GeneratePosition(request.Position, request.PositionTaskOwner),
                        Description = request.Description ?? string.Empty,
                        Created = DateTimeOffset.UtcNow,
                        CreatedBy = request.Editor.Person
                    };

                    await resourcesDb.ContractorRequests.AddAsync(newRequest);

                    var workflow = new ContractorPersonnelWorkflowV1(request.Editor.Person);
                    await resourcesDb.Workflows.AddAsync(workflow.CreateDatabaseEntity(newRequest.Id, DbRequestType.ContractorPersonnel));
                    await resourcesDb.SaveChangesAsync();

                    return newRequest;
                }


                private DbContractorRequest.RequestPosition GeneratePosition(PositionInfo position, Guid? taskOwnerPositionId) => new DbContractorRequest.RequestPosition
                {
                    AppliesFrom = position.AppliesFrom,
                    AppliesTo = position.AppliesTo,
                    BasePositionId = position.BasePositionId,
                    Name = position.PositionName,
                    Workload = position.Workload,
                    Obs = position.Obs,
                    TaskOwner = GenerateTaskOwner(taskOwnerPositionId)
                };

                private DbContractorRequest.PositionTaskOwner GenerateTaskOwner(Guid? positionId) => positionId.HasValue ? new DbContractorRequest.PositionTaskOwner
                {
                    PositionId = positionId.Value
                } : new DbContractorRequest.PositionTaskOwner();

                #region Validate

                private async Task ValidateIsOnlyActiveChangeRequestAsync(Create request)
                {
                    if (request.OriginalPositionId.HasValue)
                    {
                        var activeRequests = await resourcesDb.ContractorRequests
                            .Where(r => r.OriginalPositionId == request.OriginalPositionId)
                            .IsRunningQuery()
                            .ToListAsync();

                        if (activeRequests.Count > 0)
                        {
                            throw new RequestAlreadyExistsError("There are already requests active against the specified position");
                        }
                    }
                }

                private async Task ValidateOriginalPositionAsync(Create request)
                {
                    if (request.OriginalPositionId != null)
                    {
                        var position = await orgResolver.ResolvePositionAsync(request.OriginalPositionId.Value);

                        if (position is null)
                            throw InvalidOrgChartPositionError.NotFound(request.OriginalPositionId.Value);

                        if (position.Project.ProjectId != request.OrgProjectId)
                            throw InvalidOrgChartPositionError.InvalidProject(position);

                        if (position.ContractId != request.OrgContractId)
                            throw InvalidOrgChartPositionError.InvalidContract(position);
                    }
                }

                #endregion
            }


        }

    }



}
