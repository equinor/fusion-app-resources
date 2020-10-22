using Fusion.Integration.Notification;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using Fusion.Resources.Domain.Notifications.Request;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class RequestsNotificationHandler :
        INotificationHandler<RequestCreated>,
        INotificationHandler<RequestApprovedByCompany>,
        INotificationHandler<RequestDeclinedByCompany>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;

        public RequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
        }

        public async Task Handle(RequestCreated notification, CancellationToken cancellationToken)
        {
            var request = await GetRequest(notification.RequestId);

            if (request == null)
                return;

            await notificationClient.CreateNotificationAsync(notification => notification
                .WithRecipient(request.Workflow.WorkflowSteps.First().))

        }

        public Task Handle(RequestApprovedByCompany notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task Handle(RequestDeclinedByCompany notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<QueryPersonnelRequest> GetRequest(Guid requestId)
        {
            var query = new GetContractPersonnelRequest(requestId);
            var request = await mediator.Send(query);

            return request;
        }

        private class NotificationDescription
        {
            public static string RequestCreated(object request) => new MarkdownDocument()
                .Paragraph($"A request is ready for your approval")
                .Build();

            public static string RequestApproved(object request) => new MarkdownDocument()
                .Paragraph($"A request is ready for your approval")
                .Build();
            public static string RequestDeclined(object request) => new MarkdownDocument()
                .Paragraph($"A request is ready for your approval")
                .Build();
        }
    }
}
