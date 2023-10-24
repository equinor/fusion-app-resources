using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Behaviours
{

    public class TelemetryBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly TelemetryClient telemetryClient;

        public TelemetryBehaviour(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {

            var dependency = telemetryClient.StartOperation<DependencyTelemetry>($"{request!.GetType().Name}");
            dependency.Telemetry.Data = JsonConvert.SerializeObject(request, Formatting.Indented);
            dependency.Telemetry.Type = "CQRS";

            try
            {
                var response = await next();
                dependency.Telemetry.Success = true;

                return response;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                dependency.Telemetry.Success = false;

                throw;
            }
            finally
            {
                telemetryClient.StopOperation(dependency);
            }
        }
    }
}
