using Fusion.ApiClients.Org;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Commands;
using System.Linq;
using System.Collections.Generic;

namespace Fusion.Resources
{
    public static class IOrgApiClientExtensions
    {
        public static async Task<RequestResponse<TResponse>> GetAsync<TResponse>(this IOrgApiClient client, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(request);

            return await RequestResponse<TResponse>.FromResponseAsync(response);
        }

        public static async Task<RequestResponse<ApiPositionV2>> PatchPositionInstanceAsync(this IOrgApiClient client, ApiPositionV2 position, Guid positionInstanceId, PatchPositionInstanceV2 instance)
        {
            if (position.Id == Guid.Empty)
                throw new ArgumentException("Position id cannot be empty when updating.");

            if (position.ProjectId == Guid.Empty && (position.Project?.ProjectId == null || position.Project.ProjectId == Guid.Empty))
                throw new ArgumentException("Could not locate the project id on the position. Cannot generate url from position");


            var url = $"projects/{position.ProjectId}/positions/{position.Id}/instances/{positionInstanceId}";

            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Content = new StringContent(JsonConvert.SerializeObject(instance, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            return await RequestResponse<ApiPositionV2>.FromResponseAsync(response);
        }

        /// <summary>
        /// Resolve the task owner for a specific instance on the position.
        /// This operation will return the task owner at the start of the instance (applies from date) if no other date is specified.
        /// 
        /// The dates are validated on the instance and returns error if it is out of bounds.
        /// </summary>
        /// <param name="client">Org API Client</param>
        /// <param name="projectId">The project the position exists in</param>
        /// <param name="positionId">The position id</param>
        /// <param name="instanceId">The instance id</param>
        /// <returns>The return object is a bit different </returns>
        public static async Task<RequestResponse<ApiTaskOwnerV2?>> GetInstanceTaskOwnerAsync(this IOrgApiClient client, Guid projectId, Guid positionId, Guid instanceId)
        {
            if (positionId == Guid.Empty)
                throw new ArgumentException("Position id cannot be empty when updating.");

            var url = $"projects/{projectId}/positions/{positionId}/instances/{instanceId}/task-owner?api-version=2.0";

            return await GetAsync<ApiTaskOwnerV2?>(client, url);
        }

        public static async Task<List<ApiPositionV2>> GetReportingPath(this IOrgApiClient client, Guid projectId, Guid positionId, Guid instanceId)
        {
            var url = $"/projects/{projectId}/positions/{positionId}/instances/{instanceId}/reports-to";
            var reportsTo = await GetAsync<ApiReportsTo>(client, url);

            return reportsTo.Value.ReportPositions != null && reportsTo.Value.Path != null
                ? reportsTo.Value.ReportPositions
                    .OrderBy(pos => Array.IndexOf((Array)reportsTo.Value.Path, pos.Id))
                    .ToList()
                : new List<ApiPositionV2>();
        }
        public static async Task<ApiDraftV2> CreateProjectDraftAsync(this IOrgApiClient client, Guid projectId, string name, string? description = null)
        {
            var resp = await client.PostAsync<ApiDraftV2>($"/projects/{projectId}/drafts?api-version=2.0", new ApiDraftV2() { Name = name, Description = description });

            if (!resp.IsSuccessStatusCode)
                throw new OrgApiError(resp.Response, resp.Content);

            return resp.Value;
        }
        public static async Task<ApiDraftV2> PublishAndWaitAsync(this IOrgApiClient client, ApiDraftV2 draft)
        {
            var publishResp = await client.PostAsync<ApiDraftV2>($"/projects/{draft.ProjectId}/drafts/{draft.Id}/publish", null!);
            var publishedDraft = publishResp.Value;

            if (!publishResp.IsSuccessStatusCode)
                throw new OrgApiError(publishResp.Response, publishResp.Content);

            var response = publishResp.Response;
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                do
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    var locationUrl = response.Headers.Location?.ToString() ?? $"/drafts/{draft.Id}/publish";    // Get poll location URL

                    var checkResp = await client.GetAsync<ApiDraftV2>(locationUrl);

                    if (!checkResp.IsSuccessStatusCode)
                        throw new OrgApiError(checkResp.Response, checkResp.Content);

                    response = checkResp.Response;
                    publishedDraft = checkResp.Value;
                }
                while (response.StatusCode == HttpStatusCode.Accepted);
            }

            if (publishedDraft.Status == "PublishFailed")
                throw new DraftPublishingError(publishedDraft);

            return publishedDraft;
        }

    }

    public class RequestResponse<TResponse>
    {
        private RequestResponse(HttpResponseMessage response, string content, TResponse value)
        {
            Value = value;
            Content = content;
            Response = response;
        }
        private RequestResponse(HttpResponseMessage response, string content)
        {
            Content = content;
            Response = response;
            Value = default(TResponse)!;
        }

        public HttpStatusCode StatusCode => Response.StatusCode;
        public bool IsSuccessStatusCode => Response.IsSuccessStatusCode;

        public string Content { get; }
        public TResponse Value { get; }
        public HttpResponseMessage Response { get; }

        public static async Task<RequestResponse<TResponse>> FromResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var value = JsonConvert.DeserializeObject<TResponse>(content)!;

                return new RequestResponse<TResponse>(response, content, value);
            }

            return new RequestResponse<TResponse>(response, content);
        }
    }

    public class ApiTaskOwnerV2
    {
        /// <summary>
        /// The date used to resolve the task owner.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The position id of the task owner
        /// </summary>
        public Guid? PositionId { get; set; }

        /// <summary>
        /// Instances that are active at the date. This is usually related to rotations.
        /// Could also be delegated responsibility.
        /// </summary>
        public Guid[]? InstanceIds { get; set; }

        /// <summary>
        /// The persons assigned to the resolved instances.
        /// </summary>
        public ApiPersonV2[]? Persons { get; set; }
    }

    public class ApiReportsTo
    {
        public Guid[]? Path { get; set; }
        public ApiPositionV2[]? ReportPositions { get; set; }
    }

    public class DraftPublishingError : Exception
    {
        public ApiDraftV2 OrgChartDraft { get; set; }
        public DraftPublishingError(ApiDraftV2 draft)
            : base($"Publishing of draft resulted in error: {draft.Error?.Message}")
        {
            OrgChartDraft = draft;
        }
    }
}
