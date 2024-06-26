﻿using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Behaviours
{

    public class TrackableRequestBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IHttpContextAccessor httpContext;
        private readonly IProfileService profileServices;

        public TrackableRequestBehaviour(IHttpContextAccessor httpContext, IProfileService profileServices)
        {
            this.httpContext = httpContext;
            this.profileServices = profileServices;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
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

                    if (editor == null)
                        throw new InvalidOperationException("Could not determin editor");

                    trackableRequest.SetEditor(uniqueId, editor);
                } 
                else if (SystemEditorScope.IsEnabled.Value == true)
                {
                    // Ensure system account in db. 
                    var editor = await profileServices.EnsureSystemAccountAsync();

                    trackableRequest.SetEditor(Guid.Empty, editor);
                }
            }

            return await next();
        }
    }
}
