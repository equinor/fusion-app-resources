using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using MediatR;

namespace Fusion.Summary.Api.Domain.Commands;

public class CreateDepartment : IRequest
{
    private QueryDepartment _queryDepartment;

    public CreateDepartment(string SapDepartmentId, Guid ResourceOwnerAzureUniqueId, string FullDepartmentName)
    {
        _queryDepartment = new QueryDepartment
        {
            SapDepartmentId = SapDepartmentId,
            ResourceOwnerAzureUniqueId = ResourceOwnerAzureUniqueId,
            FullDepartmentName = FullDepartmentName
        };
    }

    public class Handler : IRequestHandler<CreateDepartment>
    {
        private readonly DatabaseContext _context;

        public Handler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task Handle(CreateDepartment request, CancellationToken cancellationToken)
        {
            var dbDepartment = DbDepartment.FromQueryDepartment(request._queryDepartment);

            _context.Departments.Add(dbDepartment);

            await _context.SaveChangesAsync();
        }
    }
}
