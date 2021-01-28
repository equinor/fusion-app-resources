using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class CreatePersonAbsence : TrackableRequest<QueryEmploymentStatus>
    {
        public CreatePersonAbsence(PersonId personId)
        {
            PersonId = personId;
        }

        private PersonId PersonId { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }

        public class Handler : IRequestHandler<CreatePersonAbsence, QueryEmploymentStatus>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryEmploymentStatus> Handle(CreatePersonAbsence request, CancellationToken cancellationToken)
            {


                var newItem = new DbPersonAbsence
                {
                    Id = Guid.NewGuid(),
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = request.Editor.Person,
                    Comment = request.Comment,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Type = Enum.Parse<DbAbsenceType>($@"{request.Type}")
                };

                await resourcesDb.PersonAbsences.AddAsync(newItem);
                await resourcesDb.SaveChangesAsync();

                return new QueryEmploymentStatus(newItem);
            }

        }
    }
}
