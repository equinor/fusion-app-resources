#nullable enable
using Fusion.Events;
using Fusion.Events.People;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Fusion.Resources.Functions.Functions
{
    public class ProfileSync
    {
        private readonly HttpClient peopleClient;
        private readonly HttpClient resourcesClient;

        public ProfileSync(IHttpClientFactory httpClientFactory)
        {
            peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
        }

        [FunctionName("profile-sync-event")]
        public async Task SyncProfile(
            [EventSubscriptionTrigger(HttpClientNames.Application.People, "subscriptions/persons", "resources-profile")] MessageContext message,
            ILogger log)
        {
            log.LogInformation("Profile sync event received");
            log.LogInformation(message.Event.Data);
            var body = message.GetBody<PeopleSubscriptionEvent>();

            if (body.Type != PeopleSubscriptionEventType.ProfileUpdated && body.Type != PeopleSubscriptionEventType.UserRemoved)
                return;

            var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{body.Person.Mail}/refresh", new
            {
                userRemoved = body.Type == PeopleSubscriptionEventType.UserRemoved

            });

            if (refreshResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                log.LogWarning($"Person with email '{body.Person.Mail}' not found in Resources");
                return;
            }

            if (!refreshResponse.IsSuccessStatusCode)
            {
                var raw = await refreshResponse.Content.ReadAsStringAsync();
                log.LogError(raw);
                refreshResponse.EnsureSuccessStatusCode();
            }

            log.LogInformation($"Successfully refreshed profile '{body.Person.Mail}'");
        }

        /// <summary>
        /// Syncing profiles at 5 am every day.
        /// </summary>
        [Singleton]
        [FunctionName("profile-sync")]
        public async Task SyncProfiles([TimerTrigger("0 0 5 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation("Synchronizing external person personnel in Resources API");

            var personnel = await GetAllExternalPersonnelAsync();

            var mailsToEnsure = personnel!
                .Where(p => !string.IsNullOrWhiteSpace(p.Mail))
                .Select(p => p.Mail)
                .Distinct()
                .ToList();

            var mailsToEnsureCount = mailsToEnsure.Count;
            if (mailsToEnsureCount == 0)
            {
                log.LogInformation($"Found {mailsToEnsureCount} profiles to ensure. Aborting...");
                return;
            }

            var ensureResult = await EnsurePersonsAsync(mailsToEnsure);

            var successCount = ensureResult.Count(x => x.Success);
            var failureCount = ensureResult.Count(x => x.Success == false);
            if (successCount > 0)
            {
                log.LogInformation($"Successfully ensured {successCount} profiles");
            }

            if (failureCount > 0)
            {
                log.LogWarning($"Failed to ensure {failureCount} profiles");
            }

            foreach (var person in ensureResult)
            {
                var resourcesPerson = personnel.First(p => p.Mail == person.Identifier);
                // Person with no change in invitation status or azure unique id may be skipped for now.
                // External personnel may receive a new account in azure. Check both for changes in invitation status and azure unique identifier.
                if (InvitationStatusMatches(person.Person?.InvitationStatus, resourcesPerson.AzureAdStatus) && person.Person?.AzureUniqueId == resourcesPerson.AzureUniquePersonId) continue;

                var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{resourcesPerson.Mail}/refresh", new
                {
                    userRemoved = !person.Success

                });

                if (refreshResponse.IsSuccessStatusCode || refreshResponse.StatusCode == HttpStatusCode.NotFound) continue;

                var content = await refreshResponse.Content.ReadAsStringAsync();
                log.LogError($"Failed to refresh '{person.Identifier}': {refreshResponse.StatusCode} - {content}");
            }
        }

        private async Task<List<ApiPersonnel>> GetAllExternalPersonnelAsync()
        {
            var retList = new List<ApiPersonnel>();
            var errorCount = 0;
            var index = 0;
            const int page = 500;
            do
            {
                var response = await resourcesClient.GetAsync($"resources/personnel?$skip={index * page}&$top={page}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    errorCount++;

                    await Task.Delay(5000);

                    // If we don't have too many errors, retry the current page
                    if (errorCount < 3)
                        continue;

                    throw new Exception(content);
                }

                var personnel = JsonConvert.DeserializeAnonymousType(content, new { Value = new List<ApiPersonnel>() });
                retList.AddRange(personnel!.Value);

                // Stop when we reached the end.
                if (personnel!.Value.Count < 1)
                    break;

                index++;
                errorCount = 0;

            } while (true);

            return retList;
        }

        private async Task<List<PersonValidationResult>> EnsurePersonsAsync(List<string> mailsToEnsure)
        {
            var peopleRequest = new HttpRequestMessage(HttpMethod.Post, $"persons/ensure");
            peopleRequest.Headers.Add("api-version", "3.0");
            var bodyContent = JsonConvert.SerializeObject(new { PersonIdentifiers = mailsToEnsure });
            peopleRequest.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

            var peopleResponse = await peopleClient.SendAsync(peopleRequest);
            peopleResponse.EnsureSuccessStatusCode();

            var peopleBody = await peopleResponse.Content.ReadAsStringAsync();
            var people = JsonConvert.DeserializeObject<List<PersonValidationResult>>(peopleBody);

            return people!;
        }

        private static bool InvitationStatusMatches(InvitationStatus? peopleStatus, ApiAccountStatus resourcesStatus)
        {
            if ((peopleStatus == InvitationStatus.Accepted && resourcesStatus == ApiAccountStatus.Available) ||
                (peopleStatus == InvitationStatus.Pending && resourcesStatus == ApiAccountStatus.InviteSent) ||
                (peopleStatus == InvitationStatus.NotSent && resourcesStatus == ApiAccountStatus.NoAccount))
            {
                return true;
            }

            return false;
        }

        public class PersonValidationResult
        {
            public bool Success { get; set; }
            public int StatusCode { get; set; }
            public string? Message { get; set; }
            public Person? Person { get; set; }
            public string? Identifier { get; set; }
        }

        public class Person
        {
            public Guid? AzureUniqueId { get; set; }
            public string? Mail { get; set; }
            public InvitationStatus? InvitationStatus { get; set; }
        }

        public class ApiPersonnel
        {
            public Guid Id { get; set; }
            public Guid? AzureUniquePersonId { get; set; }
            public string Mail { get; set; } = null!;
            public ApiAccountStatus AzureAdStatus { get; set; }
        }

        public enum InvitationStatus { Accepted, Pending, NotSent }
        public enum ApiAccountStatus { Available, InviteSent, NoAccount }



    }
}
