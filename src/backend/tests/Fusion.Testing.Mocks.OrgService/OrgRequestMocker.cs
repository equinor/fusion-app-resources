using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Fusion.Testing.Mocks.OrgService
{
    public class OrgRequestMocker
    {
        public static OrgRequestMocker Current { get; } = new();

        public List<OrgRequestInterceptor> Interceptors { get; } = new();


        public OrgRequestInterceptor Intercept(HttpMethod method, string requestPattern)
        {
            var interceptor = new OrgRequestInterceptor()
            {
                Method = method,
                RequestPattern = requestPattern
            };

            Interceptors.Add(interceptor);

            return interceptor;
        }

        public static OrgRequestInterceptor InterceptOption(string requestPattern)
        {
            var interceptor = new OrgRequestInterceptor()
            {
                Method = HttpMethod.Options,
                RequestPattern = requestPattern
            };

            lock (Current)
            {
                Current.Interceptors.Add(interceptor);
            }

            return interceptor;
        }


        public static void RemoveInterceptor(OrgRequestInterceptor interceptor)
        {
            lock (Current)
            {
                Current.Interceptors.Remove(interceptor);
            }
        }

        internal OrgRequestInterceptor GetInterceptor(HttpRequest request)
        {
            foreach (var interceptor in Interceptors)
            {
                if (!string.Equals(interceptor.Method.Method, request.Method, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Regex.IsMatch(request.Path.ToString(), interceptor.RequestPattern))
                    return interceptor;
            }

            return null;
        }
    }

}
