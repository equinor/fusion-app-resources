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
        private readonly HttpClient graphClient;

        public ProfileSync(IHttpClientFactory httpClientFactory)
        {
            peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            graphClient = httpClientFactory.CreateClient(HttpClientNames.Microsoft.Graph);
        }

        [FunctionName("profile-sync-event")]
        public async Task SyncProfile(
            [EventSubscriptionTrigger(HttpClientNames.Application.People, "subscriptions/persons", "resources-profile")] MessageContext message,
            ILogger log)
        {
            log.LogInformation("Profile sync event received");
            log.LogInformation(message.Event.Data);
            var body = message.GetBody<PeopleSubscriptionEvent>();

            if (body.Type != PeopleSubscriptionEventType.ProfileUpdated)
                return;

            var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{body.Person.Mail}/refresh", new { });

            if (refreshResponse.StatusCode == HttpStatusCode.NotFound)
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
            log.LogInformation("Profile sync starting run");

            log.LogInformation("Reading external person personnel with NoAccount or InviteSent from Resources API");

            var response = await resourcesClient.GetAsync($"resources/personnel?$filter=azureAdStatus in ('NoAccount','InviteSent')", cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var personnel = JsonConvert.DeserializeAnonymousType(body, new { Value = new List<ApiPersonnel>() }).Value;

            if (personnel.Any())
            {
                var refreshFailed = await SynchronizeProfiles(log, personnel);

                if (refreshFailed)
                    throw new Exception("Failed to refresh all profiles successfully. See log for details.");
            }

            log.LogInformation("Profiles sync run successfully completed");
        }

        /// <summary>
        /// Syncing invited profiles every 30 minute, checking for change in external user state
        /// </summary>
        [Singleton]
        [FunctionName("profile-invited-sync")]
        public async Task SyncInvitedProfiles([TimerTrigger("0 */30 * * * *", RunOnStartup = false)] TimerInfo timer, ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation("Profile invited sync starting run");
            log.LogInformation("Reading external person personnel with InviteSent from Resources API");

            var filter = $"?$filter=azureAdStatus in ('{ApiAccountStatus.InviteSent}')";
            var response = await resourcesClient.GetAsync($"resources/personnel{filter}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var personnel = JsonConvert.DeserializeAnonymousType(body, new { Value = new List<ApiPersonnel>() }).Value;

            if (personnel.Any())
            {
                log.LogInformation("Looking up personnel in MS-Graph API");
                var acceptedGraphUsers = await GetGraphGuestUsersWithAcceptedUserState(personnel, log, cancellationToken);

                log.LogInformation("Identify changed users to be refreshed");
                var toBeSynced = IdentifyUsersToBeSynchronized(personnel, acceptedGraphUsers);

                if (toBeSynced.Any())
                {
                    var refreshFailed = await SynchronizeProfiles(log, toBeSynced);

                    if (refreshFailed)
                        throw new Exception("Failed to refresh all profiles successfully. See log for details.");
                }
            }

            log.LogInformation("Profiles sync run successfully completed");
        }

        private static List<ApiPersonnel> IdentifyUsersToBeSynchronized(IEnumerable<ApiPersonnel> personnel, List<GraphUser> userList)
        {
            var list = new List<ApiPersonnel>();
            foreach (var apiPersonnel in personnel)
            {
                var userToBeSynced = userList.FirstOrDefault(x => x.Mail.Equals(apiPersonnel.Mail, StringComparison.InvariantCultureIgnoreCase));
                if (userToBeSynced != null)
                {
                    list.Add(apiPersonnel);
                }
            }
            return list;
        }

        private async Task<List<GraphUser>> GetGraphGuestUsersWithAcceptedUserState(IEnumerable<ApiPersonnel> personnel, ILogger log, CancellationToken cancellationToken)
        {
            var graphUsers = new List<GraphUser>();
            foreach (var apiPersonnel in personnel)
            {
                var resp = await graphClient.GetAsync($"/beta/users/{apiPersonnel.Mail}?$select=id,mail,userType,externalUserState", cancellationToken);
                var respData = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    log.LogWarning(respData);
                    continue;
                }
                var user = JsonConvert.DeserializeObject<GraphUser>(respData);

                if (user.ExternalUserState.Equals($"{InvitationStatus.Accepted}", StringComparison.InvariantCultureIgnoreCase) && user.UserType.Equals("Guest", StringComparison.InvariantCultureIgnoreCase))
                {
                    graphUsers.Add(user);
                }
            }
            return graphUsers;
        }

        private async Task<bool> SynchronizeProfiles(ILogger log, List<ApiPersonnel> personnel)
        {
            var mailsToEnsure = personnel
                .Where(p => !string.IsNullOrWhiteSpace(p.Mail))
                .Select(p => p.Mail)
                .Distinct()
                .ToList();

            log.LogInformation($"Found {mailsToEnsure.Count} profiles to ensure");

            var ensuredPeople = await EnsurePeople(mailsToEnsure);

            log.LogInformation($"Succesfully ensured {ensuredPeople.Count} people");

            bool refreshFailed = false;

            foreach (var person in ensuredPeople)
            {
                log.LogInformation($"Processing person '{person.Identifier}'");
                var resourcesPerson = personnel.FirstOrDefault(p => p.Mail == person.Identifier);

                if (resourcesPerson != null && !InvitationStatusMatches(person.Person.InvitationStatus, resourcesPerson.AzureAdStatus))
                {
                    log.LogInformation($"Detected change in profile '{resourcesPerson.Mail}'. Initiating refresh.");
                    var refreshResponse =
                        await resourcesClient.PostAsJsonAsync($"resources/personnel/{resourcesPerson.Mail}/refresh", new { });

                    if (refreshResponse.StatusCode == HttpStatusCode.NotFound) //not found should only be warning
                    {
                        log.LogWarning($"Person '{person.Identifier}' was not found in Azure AD");
                    }
                    else if (!refreshResponse.IsSuccessStatusCode)
                    {
                        var body = await refreshResponse.Content.ReadAsStringAsync();
                        log.LogError($"Failed to refresh '{person.Identifier}': {refreshResponse.StatusCode} - {body}");
                        refreshFailed = true;
                    }
                }
            }

            return refreshFailed;
        }

        private async Task<List<PersonValidationResult>> EnsurePeople(List<string> mailsToEnsure)
        {
            var peopleRequest = new HttpRequestMessage(HttpMethod.Post, $"persons/ensure");
            peopleRequest.Headers.Add("api-version", "2.0");
            var bodyContent = JsonConvert.SerializeObject(new { PersonIdentifiers = mailsToEnsure });
            peopleRequest.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

            var peopleResponse = await peopleClient.SendAsync(peopleRequest);
            peopleResponse.EnsureSuccessStatusCode();

            var peopleBody = await peopleResponse.Content.ReadAsStringAsync();
            var people = JsonConvert.DeserializeObject<List<PersonValidationResult>>(peopleBody);
            var ensuredPeople = people.Where(p => p.Success).ToList();
            return ensuredPeople;
        }

        private bool InvitationStatusMatches(InvitationStatus? peopleStatus, ApiAccountStatus resourcesStatus)
        {
            if ((peopleStatus == InvitationStatus.Accepted && resourcesStatus == ApiAccountStatus.Available) ||
                (peopleStatus == InvitationStatus.Pending && resourcesStatus == ApiAccountStatus.InviteSent) ||
                (peopleStatus == InvitationStatus.NotSent && resourcesStatus == ApiAccountStatus.NoAccount))
            {
                return true;
            }

            return false;
        }

        internal class GraphUser
        {
            public Guid Id { get; set; }
            public string Mail { get; set; }
            public string UserType { get; set; }
            public string ExternalUserState { get; set; }
        }

        public class PersonValidationResult
        {
            public bool Success { get; set; }
            public int StatusCode { get; set; }
            public string Message { get; set; }
            public Person Person { get; set; }
            public string Identifier { get; set; }
        }

        public class Person
        {
            public string Mail { get; set; }
            public InvitationStatus? InvitationStatus { get; set; }
        }

        public class ApiPersonnel
        {
            public Guid Id { get; set; }
            public Guid? AzureUniquePersonId { get; set; }
            public string Mail { get; set; }
            public ApiAccountStatus AzureAdStatus { get; set; }
        }

        public enum InvitationStatus { Accepted, Pending, NotSent }
        public enum ApiAccountStatus { Available, InviteSent, NoAccount }
    }
}
