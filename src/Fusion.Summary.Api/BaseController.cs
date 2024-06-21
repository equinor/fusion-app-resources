using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api;

public class BaseController : ControllerBase
{
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
