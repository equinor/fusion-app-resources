using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class RefreshPersonnel : TrackableRequest
    {
        public PersonnelId PersonellId { get; }
        public bool ConsiderRemovedProfile { get; }

        public RefreshPersonnel(PersonnelId personellId, bool considerRemovedProfile = false)
        {
            this.PersonellId = personellId;
            this.ConsiderRemovedProfile = considerRemovedProfile;
        }


        public class Handler : AsyncRequestHandler<RefreshPersonnel>
        {
            private readonly IProfileService profileService;

            public Handler(IProfileService profileService)
            {
                this.profileService = profileService;
            }

            protected override async Task Handle(RefreshPersonnel request, CancellationToken cancellationToken)
            {
                await profileService.RefreshExternalPersonnelAsync(request.PersonellId.OriginalIdentifier, request.ConsiderRemovedProfile);
            }
        }
    }
}
