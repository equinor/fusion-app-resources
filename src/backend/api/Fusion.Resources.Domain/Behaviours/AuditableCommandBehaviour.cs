using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Behaviours
{

    public class TrackableRequestBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IHttpContextAccessor httpContext;
        private readonly IProfileServices profileServices;

        public TrackableRequestBehaviour(IHttpContextAccessor httpContext, IProfileServices profileServices)
        {
            this.httpContext = httpContext;
            this.profileServices = profileServices;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request is ITrackableRequest trackableRequest)
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

            return await next();
        }
    }
}
