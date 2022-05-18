using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Requests.Sharing
{
    public class ShareRequest : TrackableRequest<bool>
    {
        public ShareRequest(Guid requestId, string scope, string source, string? reason = null)
        {
            RequestId = requestId;
            Scope = scope;
            Source = source;
            Reason = reason;
        }

        public Guid RequestId { get; set; }
        public List<PersonId> SharedWith { get; set; } = new();
        public string Scope { get; }
        public string Source { get; }
        public string? Reason { get; }

        public class Handler : IRequestHandler<ShareRequest, bool>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IProfileService profileService)
            {
                this.db = db;
                this.profileService = profileService;
            }

            public async Task<bool> Handle(ShareRequest request, CancellationToken cancellationToken)
            {
                var reason = request.Reason;
                if (string.IsNullOrEmpty(reason))
                {
                    reason = $"Shared by {request.Editor.Person.Name}";
                }

                var existingRequests = await db.SharedRequests
                    .Where(x => x.RequestId == request.RequestId && x.Source == request.Source)
                    .ToDictionaryAsync(x => x.SharedWithId, cancellationToken);

                foreach (var sharedWith in request.SharedWith)
                {
                    var person = await profileService.EnsurePersonAsync(sharedWith);
                    if (existingRequests.TryGetValue(person!.Id, out var existingRequest))
                    {
                        existingRequest.IsRevoked = true;
                        existingRequest.RevokedAt = DateTimeOffset.Now;
                    }

                    var sharedRequest = new DbSharedRequest
                    {
                        RequestId = request.RequestId,
                        SharedWithId = person!.Id,
                        SharedById = request.Editor.Person.Id,
                        Scope = request.Scope,
                        Source = request.Source,
                        Reason = reason,
                        GrantedAt = DateTimeOffset.Now,
                    };

                    db.SharedRequests.Add(sharedRequest);
                }
                var rowCount = await db.SaveChangesAsync(cancellationToken);
                return rowCount > 0;
            }
        }
    }
}
