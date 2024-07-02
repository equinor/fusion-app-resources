using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetAllDepartments : IRequest<IEnumerable<QueryDepartment>>
{
    public GetAllDepartments All()
    {
        return this;
    }

    public class Handler : IRequestHandler<GetAllDepartments, IEnumerable<QueryDepartment>>
    {
        private readonly DatabaseContext _context;

        public Handler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QueryDepartment>> Handle(GetAllDepartments request, CancellationToken cancellationToken)
        {
            var ret = new List<QueryDepartment>();

            // Get all departments 
            var dbDepartments = await _context.Departments.ToListAsync();

            // Remap
            foreach(var  dbDepartment in dbDepartments) ret.Add(QueryDepartment.FromDbDepartment(dbDepartment));

            return ret;
        }
    }
}
