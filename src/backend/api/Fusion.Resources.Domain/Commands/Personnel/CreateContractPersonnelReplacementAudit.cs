using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Commands
{

    public class CreateContractPersonnelReplacementAudit : TrackableRequest
    {

        public CreateContractPersonnelReplacementAudit(Guid projectId, Guid contractId, string message, string editor)
        {
            ProjectId = projectId;
            ContractId = contractId;
            Message = message;
            ReplacedBy = editor;
        }
        public Guid ProjectId { get; }
        public Guid ContractId { get; }
        public string Message { get; }
        public string ReplacedBy { get; }
        public string? ChangeType { get; set; }
        public string? UPN { get; set; }
        public string? FromPerson { get; set; }
        public string? ToPerson { get; set; }

        public class Handler : AsyncRequestHandler<CreateContractPersonnelReplacementAudit>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(CreateContractPersonnelReplacementAudit request, CancellationToken cancellationToken)
            {
                var change = new DbContractPersonnelReplacement
                {
                    Id = Guid.NewGuid(),
                    ProjectId = request.ProjectId,
                    ContractId = request.ContractId,
                    UPN = request.UPN,
                    FromPerson = request.FromPerson,
                    ToPerson = request.ToPerson,
                    ChangeType = request.ChangeType,
                    Message = request.Message,
                    CreatedBy = request.ReplacedBy,
                    Created = DateTimeOffset.UtcNow
                };

                db.Add(change);

                await db.SaveChangesAsync();
            }
        }
    }
}
