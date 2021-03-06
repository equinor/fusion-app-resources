﻿using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;

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
        public double? AbsencePercentage { get; set; }
        public string? TaskName { get; set; }
        public string? RoleName { get; set; }
        public string? Location { get; set; }
        public Guid? BasePositionId { get; set; }
        public bool IsPrivate { get; set; }

        public class Handler : IRequestHandler<UpdatePersonAbsence, QueryPersonAbsence>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator, IProjectOrgResolver orgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryPersonAbsence> Handle(UpdatePersonAbsence request, CancellationToken cancellationToken)
            {
                var absence = await resourcesDb.PersonAbsences
                    .GetById(request.PersonId, request.Id)
                    .Include(cp => cp.Person)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (absence is null)
                    throw new ArgumentException($"Cannot locate status using identifier '{request.Id}'");

                absence.Comment = request.Comment;
                absence.AppliesFrom = request.AppliesFrom;
                absence.AppliesTo = request.AppliesTo;
                absence.Created = DateTimeOffset.UtcNow;
                absence.CreatedBy = request.Editor.Person;
                absence.Type = Enum.Parse<DbAbsenceType>($"{request.Type}");
                absence.AbsencePercentage = request.AbsencePercentage;
                absence.IsPrivate = request.IsPrivate;

                if (absence.Type == DbAbsenceType.OtherTasks)
                {
                    var roleName = request.RoleName;
                    if (request.BasePositionId.HasValue && String.IsNullOrEmpty(request.RoleName))
                    {
                        var basePosition = await orgResolver.ResolveBasePositionAsync(request.BasePositionId.Value);
                        if (basePosition is null)
                        {
                            throw new IntegrationError("Unable to retrieve baseposition", new NullReferenceException("basePosition is null"));
                        }

                        roleName = basePosition.Name;
                    }

                    absence.TaskDetails = new DbOpTaskDetails
                    {
                        BasePositionId = request.BasePositionId,
                        TaskName = request.TaskName,
                        RoleName = roleName!,
                        Location = request.Location
                    };
                }

                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetPersonAbsenceItem(request.PersonId, request.Id));
                return returnItem!;
            }
        }
    }
}
