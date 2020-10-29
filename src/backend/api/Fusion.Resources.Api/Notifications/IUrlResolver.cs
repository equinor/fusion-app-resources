using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public interface IUrlResolver
    {
        /// <summary>
        /// Resolves url to active requests page
        /// </summary>
        /// <param name="orgProjectId">Pro org project id</param>
        /// <param name="orgContractId">Pro org contract id</param>
        /// <returns>The url or null if resolving fails</returns>
        Task<string?> ResolveActiveRequests(Guid orgProjectId, Guid orgContractId);

        /// <summary>
        /// Resolves url to manage personnel page
        /// </summary>
        /// <param name="orgProjectId">Pro org project id</param>
        /// <param name="orgContractId">Pro org contract id</param>
        /// <returns>The url or null if resolving fails</returns>
        Task<string?> ResolveManagePersonnel(Guid orgProjectId, Guid orgContractId);
    }
}