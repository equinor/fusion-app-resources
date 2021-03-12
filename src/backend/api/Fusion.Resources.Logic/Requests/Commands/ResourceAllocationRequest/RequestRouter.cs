using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class RequestRouter : INotificationHandler<RequestInitialized>
        {
            private readonly ResourcesDbContext db;

            public RequestRouter(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
            {
                if (notification.Type != DbInternalRequestType.Normal && notification.Type != DbInternalRequestType.JointVenture) return;

                var request = await db.ResourceAllocationRequests
                    .SingleAsync(r => r.Id == notification.RequestId, cancellationToken);

                if (!string.IsNullOrEmpty(request.AssignedDepartment)) return;

                var matrix = await db.ResponsibilityMatrices
                    .Include(m => m.Responsible)
                    .Include(m => m.Project)
                    .Select(m => new
                    {
                        Score = (m.Project!.Id == request.ProjectId ? 5 : 0)
                              + (m.Discipline == request.Discipline ? 2 : 0)
                              + (m.LocationId == request.OrgPositionInstance.LocationId ? 1 : 0),
                        Row = m
                    })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefaultAsync(x => x.Score >= 5, cancellationToken);

                if(matrix != null)
                {
                    request.AssignedDepartment = matrix.Row.Unit;
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
