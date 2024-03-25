using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services;

public interface IOrgClient
{
    Task<HttpResponseMessage> SavePosition(Guid projectId, Guid positionId, JObject obj, int timeoutInSeconds = 100);
    Task<HttpResponseMessage> UpdateFutureSplit(Guid projectId, Guid positionId, Guid positionInstanceId, JObject obj, int timeoutInSeconds = 100);
    Task<HttpResponseMessage> AllocateRequestInstance(Guid projectId, Guid drafId, Guid positionId, Guid positionInstanceId, JObject obj, int timeoutInSeconds = 100);
}
