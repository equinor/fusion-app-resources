using Fusion.Integration.LineOrg;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using MediatR;

namespace Fusion.Summary.Api.Domain.Commands;

public class UpdateDepartment : IRequest
{
    private QueryDepartment _queryDepartment;

    public UpdateDepartment(string sapDepartmentId, string fullDepartmentName,
        IEnumerable<Guid> resourceOwnersAzureUniqueId, IEnumerable<Guid> delegateResourceOwnersAzureUniqueId)
    {
        _queryDepartment = new QueryDepartment
        {
            SapDepartmentId = sapDepartmentId,
            FullDepartmentName = fullDepartmentName,
            ResourceOwnersAzureUniqueId = resourceOwnersAzureUniqueId.ToList(),
            DelegateResourceOwnersAzureUniqueId = delegateResourceOwnersAzureUniqueId.ToList()
        };
    }

    public class Handler : IRequestHandler<UpdateDepartment>
    {
        private readonly SummaryDbContext _context;

        public Handler(SummaryDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateDepartment request, CancellationToken cancellationToken)
        {
            var existingDepartment = await _context.Departments.FindAsync(request._queryDepartment.SapDepartmentId);

            if (existingDepartment != null)
            {
                existingDepartment.ResourceOwnersAzureUniqueId = request._queryDepartment.ResourceOwnersAzureUniqueId.ToList();
                existingDepartment.DelegateResourceOwnersAzureUniqueId = request._queryDepartment.DelegateResourceOwnersAzureUniqueId.ToList();

                await _context.SaveChangesAsync();
            }
        }
    }
}
