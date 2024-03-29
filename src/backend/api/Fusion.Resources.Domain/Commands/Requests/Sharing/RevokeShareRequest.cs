﻿using Fusion.Resources.Database;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Requests.Sharing
{
    public class RevokeShareRequest : IRequest<QuerySharedRequest?>
    {
        public RevokeShareRequest(Guid requestId, PersonId person, string source)
        {
            RequestId = requestId;
            Person = person;
            Source = source;
        }

        public Guid RequestId { get; }
        public PersonId Person { get; }
        public string Source { get; }

        public class Handler : IRequestHandler<RevokeShareRequest, QuerySharedRequest?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QuerySharedRequest?> Handle(RevokeShareRequest request, CancellationToken cancellationToken)
            {
                var sharedRequest = await db.SharedRequests
                    .Include(r => r.Request).ThenInclude(r => r.CreatedBy)
                    .Include(r => r.Request).ThenInclude(r => r.Project)
                    .Include(r => r.Request).ThenInclude(r => r.UpdatedBy)
                    .Include(r => r.SharedWith)
                    .Include(r => r.SharedBy)
                        .FirstOrDefaultAsync(x => 
                            x.RequestId == request.RequestId
                            && x.SharedWith.AzureUniqueId == request.Person.UniqueId
                            && x.Source == request.Source, cancellationToken
                        );
                if (sharedRequest is null) return null;

                sharedRequest.IsRevoked = true;
                sharedRequest.RevokedAt = DateTimeOffset.Now;

                await db.SaveChangesAsync(cancellationToken);

                return new QuerySharedRequest(sharedRequest);
            }
        }
    }
}
