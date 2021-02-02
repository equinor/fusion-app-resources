using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdatePersonAbsence : TrackableRequest<QueryPersonAbsence>
    {
        public UpdatePersonAbsence(PersonId personId, Guid id)
        {
            PersonId = personId;
            Id = id;
        }

        private PersonId PersonId { get; set; }

        private Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }
        public string? Grade { get; set; }

        public class Handler : IRequestHandler<UpdatePersonAbsence, QueryPersonAbsence>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
            }

            public async Task<QueryPersonAbsence> Handle(UpdatePersonAbsence request, CancellationToken cancellationToken)
            {
                var absences = await resourcesDb.PersonAbsences
                    .GetById(request.PersonId, request.Id)
                    .Include(cp => cp.Person)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (absences is null)
                    throw new ArgumentException($"Cannot locate status using identifier '{request.Id}'");

                absences.Comment = request.Comment;
                absences.AppliesFrom = request.AppliesFrom;
                absences.AppliesTo = request.AppliesTo;
                absences.Created = DateTimeOffset.UtcNow;
                absences.CreatedBy = request.Editor.Person;
                absences.Type = Enum.Parse<DbAbsenceType>($"{request.Type}");
                absences.Grade = request.Grade;


                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetPersonAbsenceItem(request.PersonId, request.Id));
                return returnItem!;
            }
        }
    }
}
