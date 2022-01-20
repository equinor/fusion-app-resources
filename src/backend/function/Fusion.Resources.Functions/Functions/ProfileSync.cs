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

            var personIdentifier = new ExternalPersonnelId(body.Person.AzureUniqueId, body.Person.Mail);

            var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{personIdentifier.OriginalIdentifier}/refresh", new
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
        public async Task SyncProfiles([TimerTrigger("0 0 5 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation("Synchronizing external person personnel in Resources API");

            var personnel = await GetAllActiveExternalPersonnelAsync();

            var usersToEnsure = personnel!
                .Where(p => !string.IsNullOrWhiteSpace(p.Mail))
                .Select(usr => new ExternalPersonnelId(usr.AzureUniquePersonId, usr.Mail))
                .Distinct()
                .ToList();

            if (usersToEnsure.Count == 0)
            {
                log.LogInformation($"Found no profiles to ensure. Aborting...");
                return;
            }

            // OriginalIdentifier contains AzureUniqueId if found, or Mail.  
            var ensureResult = await EnsurePersonsAsync(usersToEnsure.Select(x => x.OriginalIdentifier).ToList());

            // log ensure results
            var successCount = ensureResult.Count(x => x.Success);
            var failureCount = ensureResult.Count(x => x.Success == false);
            if (successCount > 0) log.LogInformation($"Successfully ensured {successCount} profiles");
            if (failureCount > 0) log.LogWarning($"Failed to ensure {failureCount} profiles");

            foreach (var person in ensureResult)
            {
                // person.Identifier contains AzureUniqueId if found, or Mail.   
                var personIdentifier = new ExternalPersonnelId(person.Identifier!);
                var resourcesPerson = personIdentifier.Type == ExternalPersonnelId.IdentifierType.UniqueId ? personnel.First(p => p.AzureUniquePersonId == personIdentifier.UniqueId) : personnel.First(p => p.Mail == personIdentifier.Mail);

                // Person with no change in invitation status or azure unique id may be skipped for now.
                // External personnel may receive a new account in azure. Check both for changes in invitation status and azure unique identifier.
                if (InvitationStatusMatches(person.Person?.InvitationStatus, resourcesPerson.AzureAdStatus) && person.Person?.AzureUniqueId == resourcesPerson.AzureUniquePersonId) continue;

                var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{personIdentifier.OriginalIdentifier}/refresh", new
                {
                    userRemoved = !person.Success

                });

                if (refreshResponse.IsSuccessStatusCode || refreshResponse.StatusCode == HttpStatusCode.NotFound) continue;

                var content = await refreshResponse.Content.ReadAsStringAsync();
                log.LogError($"Failed to refresh '{person.Identifier}': {refreshResponse.StatusCode} - {content}");
            }
        }

        private async Task<List<ApiPersonnel>> GetAllActiveExternalPersonnelAsync()
        {
            var retList = new List<ApiPersonnel>();
            var errorCount = 0;
            var index = 0;
            const int page = 500;
            do
            {
                var response = await resourcesClient.GetAsync($"resources/personnel?$skip={index * page}&$top={page}&$filter=isDeleted neq 'true'"); // Shouldn't consider deleted persons.
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

        private async Task<List<PersonValidationResult>> EnsurePersonsAsync(List<string> personIdentifiers)
        {
            var peopleRequest = new HttpRequestMessage(HttpMethod.Post, $"persons/ensure");
            peopleRequest.Headers.Add("api-version", "3.0");
            var bodyContent = JsonConvert.SerializeObject(new { PersonIdentifiers = personIdentifiers });
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

        public struct ExternalPersonnelId
        {
            public ExternalPersonnelId(Guid? azureUniqueId, string identifier)
            {
                if (azureUniqueId.HasValue)
                    identifier = azureUniqueId.Value.ToString();

                OriginalIdentifier = identifier;

                if (Guid.TryParse(identifier, out var id))
                {
                    UniqueId = id;
                    Mail = null;
                    Type = IdentifierType.UniqueId;
                }
                else
                {
                    UniqueId = null;
                    Mail = identifier;
                    Type = IdentifierType.Mail;
                }
            }

            public ExternalPersonnelId(string identifier)
            {

                OriginalIdentifier = identifier;

                if (Guid.TryParse(identifier, out Guid id))
                {
                    UniqueId = id;
                    Mail = null;
                    Type = IdentifierType.UniqueId;
                }
                else
                {
                    UniqueId = null;
                    Mail = identifier;
                    Type = IdentifierType.Mail;
                }
            }

            public Guid? UniqueId { get; set; }
            public string OriginalIdentifier { get; set; }
            public string? Mail { get; set; }
            public IdentifierType Type { get; set; }

            public enum IdentifierType { UniqueId, Mail }
        }
    }
}
