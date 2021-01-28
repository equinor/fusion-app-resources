using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class CreatePersonAbsence : TrackableRequest<QueryPersonAbsence>
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

        public class Handler : IRequestHandler<CreatePersonAbsence, QueryPersonAbsence>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
            }

            public async Task<QueryPersonAbsence> Handle(CreatePersonAbsence request, CancellationToken cancellationToken)
            {
                var profile = await profileService.EnsurePersonAsync(request.PersonId);
                if (profile == null)
                    throw new ArgumentException("Cannot create personnel without either a valid azure unique id or mail address");

                var newItem = new DbPersonAbsence
                {
                    Id = Guid.NewGuid(),
                    Person = profile,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = request.Editor.Person,
                    Comment = request.Comment,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Type = Enum.Parse<DbAbsenceType>($@"{request.Type}")
                };

                await resourcesDb.PersonAbsences.AddAsync(newItem);
                await resourcesDb.SaveChangesAsync();

                return new QueryPersonAbsence(newItem);
            }

        }
    }
}
