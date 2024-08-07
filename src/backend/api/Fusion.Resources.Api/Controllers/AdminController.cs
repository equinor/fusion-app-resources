﻿using System;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    public class AdminController : ResourceControllerBase
    {

        private readonly IOrgUnitCache orgUnitCache;
        private readonly IMediator mediator;

        public AdminController(IOrgUnitCache orgUnitCache, IMediator mediator)
        {

            this.orgUnitCache = orgUnitCache;
            this.mediator = mediator;
        }

        [HttpGet("admin/cache/org-units")]

        public async Task<ActionResult> CleareCache()
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.FullControl();
                    or.FullControlInternal();
                    or.BeTrustedApplication();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            await orgUnitCache.ClearOrgUnitCacheAsync();

            return Ok();
        }


        [HttpPost("admin/cache/reset-internal-cache")]
        public async Task<ActionResult> ClearInternalCache()
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.FullControl();
                    or.FullControlInternal();
                    or.BeTrustedApplication();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            await mediator.Publish(new DistributedEvents.ResetCacheNotification());
            
            return new OkObjectResult(new { message = "Cache reset has been queued for all instances."});
        }


        [HttpGet("admin/projects/sync-state")]
        public async Task<ActionResult> SyncProjectStates([FromBody] SyncProjectStatesRequest? request = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.FullControl();
                    or.FullControlInternal();
                    or.BeTrustedApplication();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            var query = new SyncProjectStates();
            if (request?.OptionalOrgProjectIdsFilter != null)
                query.WhereOrgProjectIds(request.OptionalOrgProjectIdsFilter);

            await mediator.Send(query);


            return new OkObjectResult(new { message = "Project states synced." });
        }

        public record SyncProjectStatesRequest(Guid[] OptionalOrgProjectIdsFilter);


    }
}