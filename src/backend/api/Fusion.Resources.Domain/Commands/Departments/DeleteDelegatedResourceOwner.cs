using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public  class DeleteDelegatedResourceOwner : IRequest<bool>
    {
        private readonly string departmentId;
        private readonly Guid delegatedOnwerAzureUniqueId;

        public DeleteDelegatedResourceOwner(string departmentId, Guid delegatedOnwerAzureUniqueId)
        {
            this.departmentId = departmentId;
            this.delegatedOnwerAzureUniqueId = delegatedOnwerAzureUniqueId;
        }

        public class Handler : IRequestHandler<DeleteDelegatedResourceOwner, bool>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<bool> Handle(DeleteDelegatedResourceOwner request, CancellationToken cancellationToken)
            {
                var query = db.DepartmentResponsibles
                    .Where(x => x.DepartmentId == request.departmentId
                        && x.ResponsibleAzureObjectId == request.delegatedOnwerAzureUniqueId);
                db.DepartmentResponsibles.RemoveRange(query);

                return await db.SaveChangesAsync(cancellationToken) > 0;
            }
        }
    }
}
