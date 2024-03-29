﻿using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Fusion.Resources.Functions.Integration.Http.Handlers;

public class NotificationsHttpHandler : FunctionHttpMessageHandler
{
    private readonly IOptions<HttpClientsOptions> options;

    public NotificationsHttpHandler(ILoggerFactory logger, ITokenProvider tokenProvider,
        IServiceDiscovery serviceDiscovery, IOptions<HttpClientsOptions> options)
        : base(logger.CreateLogger<NotificationsHttpHandler>(), tokenProvider, serviceDiscovery)
    {
        this.options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await SetEndpointUriForRequestAsync(request, ServiceEndpoint.Notifications);
        await AddAuthHeaderForRequestAsync(request, options.Value.Fusion);

        return await base.SendAsync(request, cancellationToken);
    }
}