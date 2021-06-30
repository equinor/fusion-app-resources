using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public class AddDelegatedResourceOwner : IRequest
    {
        public AddDelegatedResourceOwner(string departmentId, Guid responsibleAzureUniqueId)
        {
            DepartmentId = departmentId;
            ResponsibleAzureUniqueId = responsibleAzureUniqueId;
        }
        public DateTimeOffset DateFrom { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public string DepartmentId { get; }
        public Guid ResponsibleAzureUniqueId { get; }

        public class Handler : IRequestHandler<AddDelegatedResourceOwner>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<Unit> Handle(AddDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var delegatedResourceOwner = new DbDepartmentResponsible
                {
                    DateCreated = DateTime.UtcNow,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    DepartmentId = request.DepartmentId,
                    ResponsibleAzureObjectId = request.ResponsibleAzureUniqueId,
                };

                db.DepartmentResponsibles.Add(delegatedResourceOwner);
                await db.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}
