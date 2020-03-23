using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Domains.Profile
{
    public class ProfileSynchronizer
    {
        private readonly HttpClient peopleClient;
        private readonly HttpClient resourcesClient;
        private readonly ILogger<ProfileSynchronizer> logger;

        public ProfileSynchronizer(IHttpClientFactory httpClientFactory, ILogger<ProfileSynchronizer> logger)
        {
            peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            this.logger = logger;
        }

        public async Task SynchronizeAsync()
        {
            logger.LogInformation("Reading external person personnel with NoAccount or InviteSent from Resources API");

            var response = await resourcesClient.GetAsync($"resources/personnel?$filter=azureAdStatus in ('NoAccount','InviteSent', 'NotSet')");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var personnel = JsonConvert.DeserializeAnonymousType(body, new { Value = new List<ApiPersonnel>() });
            var mailsToEnsure = personnel.Value
                .Where(p => !string.IsNullOrWhiteSpace(p.Mail))
                .Select(p => p.Mail)
                .Distinct()
                .ToList();

            logger.LogInformation($"Found {mailsToEnsure.Count} profiles to ensure");

            var ensuredPeople = await EnsurePeople(mailsToEnsure);

            logger.LogInformation($"Succesfully ensured {ensuredPeople.Count} people");

            foreach (var person in ensuredPeople)
            {
                logger.LogInformation($"Processing person '{person.Identifier}'");
                var resourcesPerson = personnel.Value.FirstOrDefault(p => p.Mail == person.Identifier);

                if (!InvitationStatusMatches(person.Person.InvitationStatus, resourcesPerson.AzureAdStatus))
                {
                    logger.LogInformation($"Detected change in profile '{resourcesPerson.Mail}'. Initiating refresh.");
                    var refreshResponse = await resourcesClient.PostAsJsonAsync($"resources/personnel/{resourcesPerson.Mail}/refresh", new { });
                    refreshResponse.EnsureSuccessStatusCode();
                }
            }
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
                (peopleStatus == InvitationStatus.NotSent && resourcesStatus == ApiAccountStatus.NoAccount) ||
                (peopleStatus == null && resourcesStatus == ApiAccountStatus.NotSet))
            {
                return true;
            }

            return false;
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
        public enum ApiAccountStatus { Available, InviteSent, NoAccount, NotSet }
    }
}
