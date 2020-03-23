using Fusion.Resources.Database.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class RefreshPersonnel : TrackableRequest<QueryExternalPersonnelPerson>
    {
        private readonly PersonnelId personellId;

        public RefreshPersonnel(PersonnelId personellId)
        {
            this.personellId = personellId;
        }

        public class Handler : IRequestHandler<RefreshPersonnel, QueryExternalPersonnelPerson>
        {
            private readonly IProfileService profileService;

            public Handler(IProfileService profileService)
            {
                this.profileService = profileService;
            }

            public async Task<QueryExternalPersonnelPerson> Handle(RefreshPersonnel request, CancellationToken cancellationToken)
            {
                var profile = await profileService.RefreshExternalPersonnelAsync(request.personellId.OriginalIdentifier);
                profile.Disciplines = new List<DbPersonnelDiscipline>();

                return new QueryExternalPersonnelPerson(profile);
            }
        }
    }
}
