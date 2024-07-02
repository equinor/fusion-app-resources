using Fusion.Integration.LineOrg;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using MediatR;

namespace Fusion.Summary.Api.Domain.Commands;

public class UpdateDepartment : IRequest
{
    private QueryDepartment _queryDepartment;

    public UpdateDepartment(string SapDepartmentId, Guid ResourceOwnerAzureUniqueId, string FullDepartmentName)
    {
        _queryDepartment = new QueryDepartment
        {
            SapDepartmentId = SapDepartmentId,
            ResourceOwnerAzureUniqueId = ResourceOwnerAzureUniqueId,
            FullDepartmentName = FullDepartmentName
        };
    }

    public class Handler : IRequestHandler<UpdateDepartment>
    {
        private readonly DatabaseContext _context;

        public Handler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateDepartment request, CancellationToken cancellationToken)
        {
            var existingDepartment = await _context.Departments.FindAsync(request._queryDepartment.SapDepartmentId);

            if (existingDepartment != null)
            {
                if (existingDepartment.ResourceOwnerAzureUniqueId != request._queryDepartment.ResourceOwnerAzureUniqueId)
                {
                    existingDepartment.ResourceOwnerAzureUniqueId = request._queryDepartment.ResourceOwnerAzureUniqueId;

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
