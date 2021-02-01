using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Integration.Notification;
using Fusion.Integration.Profile;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class NotificationClientMock : IFusionNotificationClient
    {
        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, AdaptiveCard card,
            Guid? originalCreatorAzureId = null)
        {
            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, AdaptiveCard card)
        {
            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, Action<INotificationBuilder> designer)
        {
            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, Func<INotificationBuilder, Task> designer)
        {
            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, Action<INotificationBuilder> designer)
        {
            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, Func<INotificationBuilder, Task> designer)
        {
            return null;
        }

        public async Task<IEnumerable<BatchItem<FusionNotification>>> CreateNotificationForUsersAsync(IEnumerable<PersonIdentifier> persons, NotificationArguments arguments, Action<INotificationBuilder> designer)
        {
            return null;
        }

        public async Task<IEnumerable<BatchItem<FusionNotification>>> CreateNotificationForUsersAsync(IEnumerable<PersonIdentifier> persons, NotificationArguments arguments, Func<INotificationBuilder, Task> designer)
        {
            return null;
        }
    }
}