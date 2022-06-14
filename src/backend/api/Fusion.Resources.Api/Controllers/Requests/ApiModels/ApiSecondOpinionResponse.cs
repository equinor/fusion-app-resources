using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiSecondOpinionResponseStates { Open, Draft, Published, Closed }
    public class ApiSecondOpinionResponse
    {
        private readonly Guid viewerAzureUniqueId;
        private string? comment;

        public ApiSecondOpinionResponse(QuerySecondOpinionResponse response, Guid viewerAzureUniqueId, bool includeParent = true)
        {
            Id = response.Id;
            PromptId = response.PromptId;
            AssignedToId = response.AssignedToId;
            AssignedTo = new ApiPerson(response.AssignedTo);
            CreatedDate = response.CreatedDate;
            AnsweredAt = response.AnsweredAt;
            Comment = response.Comment;
            State = response.State switch
            {
                QuerySecondOpinionResponseStates.Open => ApiSecondOpinionResponseStates.Open,
                QuerySecondOpinionResponseStates.Draft => ApiSecondOpinionResponseStates.Draft,
                QuerySecondOpinionResponseStates.Published => ApiSecondOpinionResponseStates.Published,
                QuerySecondOpinionResponseStates.Closed => ApiSecondOpinionResponseStates.Closed,
                _ => throw new NotImplementedException()
            };

            if (response.SecondOpinion is not null && includeParent)
            {
                SecondOpinion = new ApiSecondOpinion(response.SecondOpinion, viewerAzureUniqueId, includeChildren: false);
            }

            this.viewerAzureUniqueId = viewerAzureUniqueId;
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public ApiPerson AssignedTo { get; set; } = null!;
        public DateTimeOffset CreatedDate { get; }

        public DateTimeOffset? AnsweredAt { get; set; }


        public string? Comment
        {
            get
            {
                if (viewerAzureUniqueId == AssignedTo.AzureUniquePersonId || State == ApiSecondOpinionResponseStates.Published)
                {
                    return comment;
                }
                return "";
            }
            set
            {
                comment = value;
            }
        }
        public ApiSecondOpinionResponseStates State { get; set; }
        public ApiSecondOpinion? SecondOpinion { get; }

    }
}
