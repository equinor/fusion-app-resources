using Fusion.Resources.Functions.Domains.Profile;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class ProfileSync
    {
        private readonly ProfileSynchronizer profileSynchronizer;

        public ProfileSync(ProfileSynchronizer profileSynchronizer)
        {
            this.profileSynchronizer = profileSynchronizer;
        }

        /// <summary>
        /// Syncing profiles at 5 am every day.
        /// </summary>
        [Singleton]
        [FunctionName("profile-sync")]
        public async Task SyncProfiles([TimerTrigger("0 0 5 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation("Profile sync starting run");

            await profileSynchronizer.SynchronizeAsync();

            log.LogInformation("Profiles sync run completed");
        }
    }
}
