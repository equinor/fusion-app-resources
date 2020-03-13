using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class ProfileSync
    {
        public ProfileSync()
        {

        }

        public async Task SyncProfiles([TimerTrigger("* * * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken cancellationToken)
        {

        }
    }
}
