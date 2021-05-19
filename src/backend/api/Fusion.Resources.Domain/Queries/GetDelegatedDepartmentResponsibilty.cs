using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetDelegatedDepartmentResponsibilty : IRequest<IEnumerable<QueryDepartmentResponsible>>
    {
        private readonly Guid? azureAdObjectId;
        public DateTime AtDate { get; set; } = DateTime.Now;

        public GetDelegatedDepartmentResponsibilty(Guid? azureAdObjectId)
        {
            this.azureAdObjectId = azureAdObjectId;
        }

        public IQueryable<QueryDepartmentResponsible> Execute(IQueryable<DbDepartmentResponsible> responsibles)
        {
            return responsibles
                .Where(r => r.ResponsibleAzureObjectId == azureAdObjectId)
                .Where(r => r.DateFrom < AtDate && r.DateTo > AtDate)
                .Select(r => new QueryDepartmentResponsible(r));
        }

        public class Handler : IRequestHandler<GetDelegatedDepartmentResponsibilty, IEnumerable<QueryDepartmentResponsible>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<IEnumerable<QueryDepartmentResponsible>> Handle(GetDelegatedDepartmentResponsibilty request, CancellationToken cancellationToken)
            {
                return await request.Execute(db.DepartmentResponsibles).ToListAsync(cancellationToken);
            }
        }
    }
}
