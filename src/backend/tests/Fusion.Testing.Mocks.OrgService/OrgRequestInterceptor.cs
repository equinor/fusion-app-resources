using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.OrgService
{
    public class OrgRequestInterceptor : IDisposable
    {
        public HttpMethod Method { get; set; }
        public string RequestPattern { get; set; }

        public Func<string, HttpContext, Task> Processor { get; set; }


        public OrgRequestInterceptor RespondWithHeaders(HttpStatusCode code, Action<IHeaderDictionary> headers)
        {
            Processor = (body, context) =>
            {
                context.Response.StatusCode = (int)code;
                headers(context.Response.Headers);
                return Task.CompletedTask;
            };

            return this;
        }

        public void Dispose()
        {
            OrgRequestMocker.RemoveInterceptor(this);
        }
    }

}
