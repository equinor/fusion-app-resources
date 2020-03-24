using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class RefreshPersonnel : TrackableRequest
    {
        private readonly PersonnelId personellId;

        public RefreshPersonnel(PersonnelId personellId)
        {
            this.personellId = personellId;
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
                await profileService.RefreshExternalPersonnelAsync(request.personellId.OriginalIdentifier);
            }
        }
    }
}
