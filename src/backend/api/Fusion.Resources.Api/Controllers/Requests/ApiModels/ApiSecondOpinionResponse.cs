using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers.Requests
{
    public enum ApiSecondOpinionResponseState { Open, Draft, Published }
    public class ApiSecondOpinionResponse
    {
        public ApiSecondOpinionResponse(QuerySecondOpinionResponse response)
        {
            Id = response.Id;
            PromptId = response.PromptId;
            AssignedToId = response.AssignedToId;
            AssignedTo = new ApiPerson(response.AssignedTo);

            AnsweredAt = response.AnsweredAt;
            Comment = response.Comment;
            State = response.State switch
            {
                QuerySecondOpinionResponseStates.Open => ApiSecondOpinionResponseState.Open,
                QuerySecondOpinionResponseStates.Draft => ApiSecondOpinionResponseState.Draft,
                QuerySecondOpinionResponseStates.Published => ApiSecondOpinionResponseState.Published,
                _ => throw new NotImplementedException()
            };
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public ApiPerson AssignedTo { get; set; } = null!;


        public DateTimeOffset? AnsweredAt { get; set; }

        public string? Comment { get; set; }
        public ApiSecondOpinionResponseState State { get; set; }
    }
}
