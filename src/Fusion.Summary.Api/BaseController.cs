using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api;

public class BaseController : ControllerBase
{
    // TODO: Transition to ProblemDetails
    protected NotFoundObjectResult DepartmentNotFound(string sapDepartmentId) =>
        NotFound(new { message = $"Department with id '{sapDepartmentId}' was not found" });


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