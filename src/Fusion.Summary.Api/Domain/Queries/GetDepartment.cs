using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetDepartment : IRequest<QueryDepartment?>
{
    public string SapDepartmentId { get; set; }

    public GetDepartment(string sapDepartmentId)
    {
        SapDepartmentId = sapDepartmentId;
    }

    public class Handler : IRequestHandler<GetDepartment, QueryDepartment?>
    {
        private readonly DatabaseContext _context;

        public Handler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<QueryDepartment?> Handle(GetDepartment request, CancellationToken cancellationToken)
        {
            // Filter
            var dbDepartment = await _context.Departments.FirstOrDefaultAsync(x=>x.DepartmentSapId == request.SapDepartmentId);

            // Nullcheck
            if (dbDepartment == null) return null;

            // Convert
            var department = QueryDepartment.FromDbDepartment(dbDepartment);

            return department;
        }
    }
}
