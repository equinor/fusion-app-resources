using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal static class NotificationClientMockExtensions
    {
        // Leaving in these extensions for reference.
        // It seems fetching notifications by number gives intermitten false errors when running all tests
        // Should instead use the request id to fetch notifications, as this provides better uniqueness.

        //public static List<NotificationClientMock.Notification> GetNotificationsForRequestNumber(this List<NotificationClientMock.Notification> notifications, long number)
        //    => GetNotificationsForRequestNumber(notifications, $"{number}");

        //public static List<NotificationClientMock.Notification> GetNotificationsForRequestNumber(this List<NotificationClientMock.Notification> notifications, int number)
        //    => GetNotificationsForRequestNumber(notifications, $"{number}");

        //public static List<NotificationClientMock.Notification> GetNotificationsForRequestNumber(this List<NotificationClientMock.Notification> notifications, string number)
        //{
        //    return notifications
        //        // Look for cards where there is a fact entry which specify the request number
        //        .Where(n => n.Card.Body
        //            .OfType<AdaptiveFactSet>()
        //            .SelectMany(x => x.Facts)
        //            .Where(x => x.Title?.Contains("Request number", StringComparison.OrdinalIgnoreCase) == true && x.Value == number)
        //            .Any())
        //        .ToList();
        //}

        public static List<NotificationClientMock.Notification> GetNotificationsForRequestId(this List<NotificationClientMock.Notification> notifications, Guid requestId)
        {
            return notifications
                // Look for cards where there is a fact entry which specify the request number
                .Where(n => n.Card.AdditionalProperties.Any(kv => kv.Key == "requestId" && kv.Value?.ToString() == $"{requestId}"))
                .ToList();
        }
    }
}