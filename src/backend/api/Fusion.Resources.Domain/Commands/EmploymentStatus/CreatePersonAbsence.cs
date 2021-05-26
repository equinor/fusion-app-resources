using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public double? AbsencePercentage { get; set; }
        public bool IsPrivate { get; set; }
        public string? TaskName { get; set; }
        public string? RoleName { get; set; }
        public string? Location { get; set; }
        public Guid? BasePositionId { get; set; }

        public class Handler : IRequestHandler<CreatePersonAbsence, QueryPersonAbsence>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;
            private readonly IOrgApiClient orgApiClient;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IOrgApiClientFactory orgApiClientFactory)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
                this.orgApiClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
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
                    AppliesFrom = request.AppliesFrom.Date,
                    AppliesTo = request.AppliesTo?.Date,
                    Type = Enum.Parse<DbAbsenceType>($@"{request.Type}"),
                    AbsencePercentage = request.AbsencePercentage,
                    IsPrivate = request.IsPrivate
                };

                if(request.Type == QueryAbsenceType.OtherTasks)
                {
                    var roleName = request.RoleName;
                    if (request.BasePositionId.HasValue && String.IsNullOrEmpty(request.RoleName))
                    {
                        var basePosition = await orgApiClient.GetAsync<ApiBasePositionV2>($"/positions/basepositions/{request.BasePositionId}");
                        if(!basePosition.IsSuccessStatusCode)
                        {
                            throw new IntegrationError("Unable to retrieve baseposition", new OrgApiError(basePosition.Response, basePosition.Content));
                        }

                        roleName = basePosition.Value.Name;
                    }

                    newItem.TaskDetails = new DbTaskDetails
                    {
                        BasePositionId = request.BasePositionId,
                        TaskName = request.TaskName,
                        RoleName = roleName!,
                        Location = request.Location
                    };
                }

                resourcesDb.PersonAbsences.Add(newItem);
                await resourcesDb.SaveChangesAsync(cancellationToken);

                return new QueryPersonAbsence(newItem);
            }
            
        }
    }
}
