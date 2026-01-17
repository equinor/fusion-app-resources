using Fusion.Integration.Org;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;

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

        // This does not work for event transaction atm. 
        // This is due to async local being used and by having an async function create the scope, causes it to be made in a parallell async tree.
        // Could have been done by creating a handler instead..

        protected Task<IDbContextTransaction> BeginTransactionAsync()
        {
            var scope = HttpContext.RequestServices.GetRequiredService<ITransactionScope>();
            return scope.BeginTransactionAsync();
        }

        protected Task DispatchCommandAsync(IRequest command)
        {
            var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
            return mediator.Send(command);
        }
        protected Task<TResult> DispatchAsync<TResult>(IRequest<TResult> command)
        {
            var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
            return mediator.Send(command);
        }
        protected Task DispatchAsync(INotification notification)
        {
            var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();
            return mediator.Publish(notification);
        }

        protected Task<ApiPositionV2?> ResolvePositionAsync(Guid positionId)
        {
            var orgResolver = HttpContext.RequestServices.GetRequiredService<IProjectOrgResolver>();
            return orgResolver.ResolvePositionAsync(positionId);
        }

        protected Task<ApiProjectV2?> ResolveProjectAsync(Guid projectId)
        {
            var orgResolver = HttpContext.RequestServices.GetRequiredService<IProjectOrgResolver>();
            return orgResolver.ResolveProjectAsync(projectId);
        }

        protected async Task<(bool isDisabled, ActionResult? response)> IsChangeRequestsDisabledAsync(Guid orgProjectId)
        {
            var project = await ResolveProjectAsync(orgProjectId);

            if (project is null)
                throw new InvalidOperationException("Could not locate project");

            if (project.Properties.GetProperty<bool>("resourceOwnerRequestsEnabled", false))
                return (false, null);

            var writeEnabled = project.Properties.GetProperty<bool>("pimsWriteSyncEnabled", false);
            if (writeEnabled)
                return (false, null);

            return (true, ApiErrors.InvalidOperation("ChangeRequestsDisabled", "The project does not currently support change requests from resource owners..."));
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
