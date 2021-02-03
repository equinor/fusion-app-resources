using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Integration.Notification;
using Fusion.Integration.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class NotificationClientMock : IFusionNotificationClient
    {
        private readonly IServiceProvider serviceProvider;

        private static List<Notification> items = new List<Notification>();

        public static List<Notification> SentMessages => items;
        public NotificationClientMock(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Wait for notification to appear in the mock notification client. Will wait for x seconds and return the notification found.
        /// 
        /// Returns null if timeout is reached.
        /// </summary>
        /// <param name="timeoutSeconds">Seconds to wait before returning</param>
        /// <param name="selector">Identify the notification to fetch.</param>
        /// <returns>The notification matching the selector or null if timout is reached</returns>
        public static async Task<Notification> WaitForNotificationAsync(int timeoutSeconds, Func<Notification, bool> selector)
        {
            using var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            while (!SentMessages.Any(selector))
            {
                if (cancelSource.IsCancellationRequested)
                    break;

                await Task.Delay(100);
            }

            return NotificationClientMock.SentMessages.FirstOrDefault(selector);
        }

        public Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, AdaptiveCard card, Guid? originalCreatorAzureId = null)
        {
            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = title,
                    Card = card,
                    OriginalCreatorUniqueId = originalCreatorAzureId
                });
            }

            return Task.FromResult<FusionNotification>(null);
        }

        public Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, AdaptiveCard card)
        {
            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = arguments.Title,
                    Card = card,
                    OriginalCreatorUniqueId = arguments.OriginalCreatorAzureId
                });
            }

            return Task.FromResult<FusionNotification>(null);
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, Action<INotificationBuilder> designer)
        {
            var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
            utils.SetLocale(DesignerLocale.Default);

            var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

            designer(builder);

            var card = await builder.BuildCardAsync();

            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = title,
                    Card = card,
                    OriginalCreatorUniqueId = builder.OriginalCreatorAzureId
                });
            }

            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, string title, Func<INotificationBuilder, Task> designer)
        {
            var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
            utils.SetLocale(DesignerLocale.Default);

            var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

            await designer(builder);

            var card = await builder.BuildCardAsync();

            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = title,
                    Card = card,
                    OriginalCreatorUniqueId = builder.OriginalCreatorAzureId
                });
            }

            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, Action<INotificationBuilder> designer)
        {
            var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
            utils.SetLocale(DesignerLocale.Default);

            var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

            designer(builder);

            var card = await builder.BuildCardAsync();

            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = arguments.Title,
                    Card = card,
                    OriginalCreatorUniqueId = builder.OriginalCreatorAzureId ?? arguments.OriginalCreatorAzureId
                });
            }

            return null;
        }

        public async Task<FusionNotification> CreateNotificationForUserAsync(PersonIdentifier person, NotificationArguments arguments, Func<INotificationBuilder, Task> designer)
        {
            var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
            utils.SetLocale(DesignerLocale.Default);

            var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

            await designer(builder);

            var card = await builder.BuildCardAsync();

            lock (SentMessages)
            {
                SentMessages.Add(new Notification
                {
                    PersonIdentifier = person.ToString(),
                    Title = arguments.Title,
                    Card = card,
                    OriginalCreatorUniqueId = builder.OriginalCreatorAzureId ?? arguments.OriginalCreatorAzureId
                });
            }

            return null;
        }

        public async Task<IEnumerable<BatchItem<FusionNotification>>> CreateNotificationForUsersAsync(IEnumerable<PersonIdentifier> persons, NotificationArguments arguments, Action<INotificationBuilder> designer)
        {
            foreach (var person in persons)
            {
                var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
                utils.SetLocale(DesignerLocale.Default);

                var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

                designer(builder);

                var card = await builder.BuildCardAsync();

                lock (SentMessages)
                {
                    SentMessages.Add(new Notification
                    {
                        PersonIdentifier = person.ToString(),
                        Title = arguments.Title,
                        Card = card,
                        OriginalCreatorUniqueId = builder.OriginalCreatorAzureId ?? arguments.OriginalCreatorAzureId
                    });
                }

            }
            return persons.Select(p => new BatchItem<FusionNotification>()
            {
                Item = null,
                StatusCode = System.Net.HttpStatusCode.Created
            });
        }

        public async Task<IEnumerable<BatchItem<FusionNotification>>> CreateNotificationForUsersAsync(IEnumerable<PersonIdentifier> persons, NotificationArguments arguments, Func<INotificationBuilder, Task> designer)
        {
            foreach (var person in persons)
            {
                var utils = serviceProvider.GetRequiredService<IBuilderUtils>();
                utils.SetLocale(DesignerLocale.Default);

                var builder = new FusionNotificationBuilder(new TestBuilderUtils(utils));

                await designer(builder);

                var card = await builder.BuildCardAsync();

                lock (SentMessages)
                {
                    SentMessages.Add(new Notification
                    {
                        PersonIdentifier = person.ToString(),
                        Title = arguments.Title,
                        Card = card,
                        OriginalCreatorUniqueId = builder.OriginalCreatorAzureId ?? arguments.OriginalCreatorAzureId
                    });
                }

            }
            return persons.Select(p => new BatchItem<FusionNotification>()
            {
                Item = null,
                StatusCode = System.Net.HttpStatusCode.Created
            });
        }

        public class Notification
        {
            public string PersonIdentifier { get; set; }
            public string Title { get; set; }
            public AdaptiveCard Card { get; set; }
            public Guid? OriginalCreatorUniqueId { get; set; }
        }

        
    }
}