using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api;

public class BaseController : ControllerBase
{
    protected ActionResult DepartmentNotFound(string sapDepartmentId) =>
        FusionApiError.NotFound(sapDepartmentId, $"Department with sap id '{sapDepartmentId}' was not found");

    protected ActionResult ProjectNotFound(Guid projectId) =>
        FusionApiError.NotFound(projectId, $"Project with id or externalId '{projectId}' was not found");

    protected ActionResult SapDepartmentIdRequired() =>
        FusionApiError.InvalidOperation("SapDepartmentIdRequired", "SapDepartmentId route parameter is required");


    protected Task DispatchAsync(IRequest command)
    {
        var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();

        return mediator.Send(command, HttpContext.RequestAborted);
    }

    protected Task<TResult> DispatchAsync<TResult>(IRequest<TResult> command)
    {
        var mediator = HttpContext.RequestServices.GetRequiredService<IMediator>();

        return mediator.Send(command, HttpContext.RequestAborted);
    }

    protected async Task<FusionPersonProfile?> ResolvePersonAsync(PersonIdentifier personId)
    {
        var profileResolver = HttpContext.RequestServices.GetRequiredService<IFusionProfileResolver>();
        return await profileResolver.ResolvePersonBasicProfileAsync(personId);
    }

    protected async Task<IEnumerable<ResolvedPersonProfile>> ResolvePersonsAsync(IEnumerable<PersonIdentifier> personIdentifiers)
    {
        var profileResolver = HttpContext.RequestServices.GetRequiredService<IFusionProfileResolver>();
        return await profileResolver.ResolvePersonsAsync(personIdentifiers);
    }
}