using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Http.Models;
using Fusion.Integration.Org;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Domain.Services.OrgClient.Abstractions;

public interface IOrgApiClient
{
    /// <summary>
    ///     Internal HttpClient used by the org client
    /// </summary>
    public HttpClient HttpClient { get; }

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    public Task<ApiPositionV2> GetPositionV2Async(OrgProjectId projectId, Guid positionId, ODataQuery? query = null);
}