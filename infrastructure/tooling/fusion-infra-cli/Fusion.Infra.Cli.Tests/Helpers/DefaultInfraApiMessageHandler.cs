using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Fusion.Infra.Cli.Tests
{
    public class DefaultInfraApiMessageHandler : HttpMessageHandler
    {



        public List<TestInfraOperation> Operations { get; set; } = new List<TestInfraOperation>();
        public bool Autocomplete { get; set; } = true;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri!.ToString();


            if (Regex.IsMatch(url, "/sql-servers/(non-|)production/databases"))
            {
                // Require payload here.
                if (request.Content is null)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                }

                var data = await request.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<ApiDatabaseRequestModel>(data);

                var operation = new TestInfraOperation()
                {
                    ProductionEnvironment = Regex.IsMatch(url, "/sql-servers/production/"),
                    Request = model,
                    Id = $"{Guid.NewGuid()}"
                };

                Operations.Add(operation);

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.Accepted)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new ApiOperationResponse()
                    {
                        DatabaseName = $"fusion-app-{model?.Name}-DB",
                        Id = operation.Id,
                        Status = ApiOperationResponse.STATUS_NEW,
                        CreatedAt = DateTime.Now
                    }, Formatting.Indented), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")),
                };
                response.Headers.Add("Location", $"/operations/{operation.Id}");

                // Set operation to completed
                return response;
            }

            if (Regex.IsMatch(url, "/operations/.+"))
            {
                var match = Regex.Match(url, "/operations/([^/]+)");
                var operationId = match.Groups[1].Value;

                var operation = Operations.FirstOrDefault(o => o.Id == operationId);
                if (operation is null)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }

                operation.Checks++;

                if (Autocomplete)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new ApiOperationResponse()
                        {
                            DatabaseName = $"fusion-app-{operation.Request?.Name}-DB",
                            Id = operation.Id,
                            Status = ApiOperationResponse.STATUS_COMPLETED,
                            CreatedAt = DateTime.Now.AddSeconds(-5),
                            CompletedAt = DateTime.Now
                        }, Formatting.Indented), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
                    };
                }
                else
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new ApiOperationResponse()
                        {
                            DatabaseName = $"fusion-app-{operation.Request?.Name}-DB",
                            Id = operation.Id,
                            Status = ApiOperationResponse.STATUS_NEW,
                            CreatedAt = DateTime.Now.AddSeconds(-5),
                        }, Formatting.Indented), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
                    };
                }
            }

            throw new NotSupportedException("Request not supported");
            //return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }


}