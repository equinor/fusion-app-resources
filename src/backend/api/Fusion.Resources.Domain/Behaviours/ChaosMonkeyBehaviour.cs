using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Behaviours
{
    public class ChaosMonkeyBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IHttpContextAccessor httpContext;
        private readonly IProfileService profileServices;

        public ChaosMonkeyBehaviour(IHttpContextAccessor httpContext, IProfileService profileServices)
        {
            this.httpContext = httpContext;
            this.profileServices = profileServices;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request is ITrackableRequest trackableRequest)
            {
                if (httpContext.HttpContext != null)
                {
                    var user = httpContext.HttpContext.User;
                    var uniqueId = user.GetAzureUniqueIdOrThrow();

                    var editor = user.IsApplicationUser() switch
                    {
                        true => await profileServices.EnsureApplicationAsync(uniqueId),
                        _ => await profileServices.EnsurePersonAsync(uniqueId)
                    };

                    trackableRequest.SetEditor(uniqueId, editor);
                }
            }

            return await next();
        }
    }
}
