﻿using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetAllDepartments : IRequest<IEnumerable<QueryDepartment>>
{
    public class Handler : IRequestHandler<GetAllDepartments, IEnumerable<QueryDepartment>>
    {
        private readonly SummaryDbContext _context;

        public Handler(SummaryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QueryDepartment>> Handle(GetAllDepartments request, CancellationToken cancellationToken)
        {
            var ret = new List<QueryDepartment>();

            // Get all departments 
            var dbDepartments = await _context.Departments.ToListAsync(cancellationToken: cancellationToken);

            // Remap
            foreach(var  dbDepartment in dbDepartments) ret.Add(QueryDepartment.FromDbDepartment(dbDepartment));

            return ret;
        }
    }
}
