using Fusion.Infra.Cli.Tests;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Fusion.Infra.Cli.Mocks
{

    public class GraphApiMessageHandler : HttpMessageHandler
    {
        public List<ServicePrincipalAppReg> ServicePrincipalRegistrations { get; set; } = new List<ServicePrincipalAppReg>();


        public List<TestInfraOperation> Operations { get; set; } = new List<TestInfraOperation>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            string url = request.RequestUri!.ToString();

            var findAppByIdMatch = Regex.Match(url, @"/v1\.0/servicePrincipals\(appId='([^']+)'\)");
            var searchForApp = Regex.Match(url, @"/v1\.0/servicePrincipals\?\$filter=displayName eq '([^']+)'");

            if (request.Method == HttpMethod.Get && findAppByIdMatch.Success)
            {
                if (!Guid.TryParse(findAppByIdMatch.Groups[1].Value, out Guid appId))
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

                var sp = ServicePrincipalRegistrations.FirstOrDefault(sp => sp.AppId == appId);
                if (sp is null)
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);


                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(sp, Formatting.Indented), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
                };
            }

            if (request.Method == HttpMethod.Get && searchForApp.Success)
            {
                var searchString = searchForApp.Groups[1].Value;

                var sp = ServicePrincipalRegistrations.FirstOrDefault(sp => string.Equals(sp.DisplayName, searchString, StringComparison.OrdinalIgnoreCase));
                if (sp is null)
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(sp, Formatting.Indented), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
                };
            }

            await Task.Delay(1);
            throw new NotSupportedException("Request not supported");
        }
    }


}