using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api;

public class BaseController : ControllerBase
{
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
}
