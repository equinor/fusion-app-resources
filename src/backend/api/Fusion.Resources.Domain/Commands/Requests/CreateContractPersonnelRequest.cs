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

namespace Fusion.Resources.Domain.Commands
{
  

    public class CreateContractPersonnelRequest : TrackableRequest<QueryPersonnelRequest>
    {
        public CreateContractPersonnelRequest(Guid projectId, Guid contractIdentifier)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
        }


        public Guid OrgContractId { get; set; }
        public Guid OrgProjectId { get; set; }
        public PersonId Person { get; set; }


        public Guid BasePositionId { get; set; }
        public string PositionName { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }

        public string Description { get; set; }

        public Guid? TaskOwnerPositionId { get; set; }


        public class Handler : IRequestHandler<CreateContractPersonnelRequest, QueryPersonnelRequest>
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
            public async Task<QueryPersonnelRequest> Handle(CreateContractPersonnelRequest request, CancellationToken cancellationToken)
            {
                // Validate references.
                var project = await resourcesDb.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == request.OrgProjectId);
                var contract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == request.OrgContractId);

                if (project is null)
                    throw new InvalidOperationException("Could not locate the project, does it have any contracts allocated?");

                if (contract is null)
                    throw new InvalidOperationException($"Cannot create personnel to unallocated contracts. Could not locate any contracts with id {request.OrgContractId}.");

                var personnel = await profileService.ResolveExternalPersonnelAsync(request.Person);

                if (personnel is null)
                    throw new InvalidOperationException("The person does not exist in the personnel register. Include him/her before creating a request.");

                var contractPersonnel = await resourcesDb.ContractPersonnel.FirstOrDefaultAsync(p => p.ProjectId == project.Id && p.ContractId == contract.Id && p.PersonId == personnel.Id);

                if (contractPersonnel is null)
                    throw new InvalidOperationException("The person was located as contractor personnel, but has not been added to this contract.");
                

                var newRequest = new DbContractorRequest()
                {
                    Contract = contract,
                    Project = project,                    
                    State = DbRequestState.Created,
                    Person = contractPersonnel,
                    Position = GeneratePosition(request),
                    Description = request.Description,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = request.Editor.Person
                };

                await resourcesDb.ContractorRequests.AddAsync(newRequest);
                await resourcesDb.SaveChangesAsync();


                var personnelRequest = await mediator.Send(new GetContractPersonnelRequest(newRequest.Id));
                return personnelRequest;
            }

            private DbContractorRequest.RequestPosition GeneratePosition(CreateContractPersonnelRequest request) => new DbContractorRequest.RequestPosition
            {
                AppliesFrom = request.AppliesFrom,
                AppliesTo = request.AppliesTo,
                BasePositionId = request.BasePositionId,
                Name = request.PositionName,
                Workload = request.Workload,
                TaskOwner = GenerateTaskOwner(request)
            };

            private DbContractorRequest.PositionTaskOwner GenerateTaskOwner(CreateContractPersonnelRequest request) => request.TaskOwnerPositionId.HasValue ? new DbContractorRequest.PositionTaskOwner
            {
                PositionId = request.TaskOwnerPositionId.Value
            } : null;
        }
    }
}
