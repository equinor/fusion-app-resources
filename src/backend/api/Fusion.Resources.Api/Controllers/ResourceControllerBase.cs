using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public class ResourceControllerBase : ControllerBase
    {
        public FusionFullPersonProfile? UserFusionProfile
        {
            get
            {
                if (HttpContext.Items.ContainsKey("FusionProfile"))
                {
                    var profile = HttpContext.Items["FusionProfile"] as FusionFullPersonProfile;

                    if (profile != null && profile.Roles != null)
                    {
                        return profile;
                    }
                }

                return null;
            }
        }

        private CommandDispatcher? dispatcher = null;

        public CommandDispatcher Commands
        {
            get
            {
                if (dispatcher is null)
                {
                    var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
                    dispatcher = new CommandDispatcher(mediator);
                }

                return dispatcher;
            }
        }

        protected Task<IDbContextTransaction> BeginTransactionAsync()
        {
            var scope = HttpContext.RequestServices.GetRequiredService<ITransactionScope>();
            return scope.BeginTransactionAsync();
        }

        protected Task DispatchAsync(IRequest command)
        {
            var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
            return mediator.Send(command);
        }
        protected Task<TResult> DispatchAsync<TResult>(IRequest<TResult> command)
        {
            var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
            return mediator.Send(command);
        }

        protected Task<ApiPositionV2?> ResolvePositionAsync(Guid positionId)
        {
            var orgResolver = HttpContext.RequestServices.GetRequiredService<IProjectOrgResolver>();
            return orgResolver.ResolvePositionAsync(positionId);
        }

        public class CommandDispatcher
        {
            public readonly IMediator mediator;

            public CommandDispatcher(IMediator mediator)
            {
                this.mediator = mediator;
            }

            public Task DispatchAsync(IRequest command)
            {
                return mediator.Send(command);
            }

            public Task<TResult> DispatchAsync<TResult>(IRequest<TResult> command)
            {
                return mediator.Send(command);
            }
        }
    }
}
