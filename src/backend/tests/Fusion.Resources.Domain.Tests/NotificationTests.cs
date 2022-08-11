using FluentAssertions;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Test.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Domain.Tests
{
    public class NotificationTests : DbTestFixture
    {
        public class SecondOpinionNotificationTestHandler : INotificationHandler<SecondOpinionRequested>
        {
            public bool WasReceived { get; set; } = false;
            public Guid? SecondOpinionId { get; set; } = null;
            public Task Handle(SecondOpinionRequested notification, CancellationToken cancellationToken)
            {
                WasReceived = true;
                SecondOpinionId = notification.SecondOpinion.Id;

                return Task.CompletedTask;
            }
        }

        public override void ConfigureServices(ServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<INotificationHandler<SecondOpinionRequested>, SecondOpinionNotificationTestHandler>();
        }

        [Fact]
        public async Task SecondOpinionRequested_ShouldHaveIdSet()
        {
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var testHandler = serviceProvider.GetRequiredService<INotificationHandler<SecondOpinionRequested>>() as SecondOpinionNotificationTestHandler;

            var request = await AddRequest();
            var command = new AddSecondOpinion(request.Id, "Please provide your input", "description", new[] { new PersonId("lorv@equinor.com") });

            var addedSecondOpinion = await mediator.Send(command);

            testHandler.WasReceived.Should().BeTrue();
            testHandler.SecondOpinionId.Should().NotBeEmpty();
            testHandler.SecondOpinionId.Should().Be(addedSecondOpinion.Id);
        }
    }
}
