using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public enum ApiSecondOpinionResponseStates { Open, Draft, Published }
    public class ApiSecondOpinionResponse
    {
        public ApiSecondOpinionResponse(QuerySecondOpinionResponse response, bool includeParent = true)
        {
            Id = response.Id;
            PromptId = response.PromptId;
            AssignedToId = response.AssignedToId;
            AssignedTo = new ApiPerson(response.AssignedTo);

            AnsweredAt = response.AnsweredAt;
            Comment = response.Comment;
            State = response.State switch
            {
                QuerySecondOpinionResponseStates.Open => ApiSecondOpinionResponseStates.Open,
                QuerySecondOpinionResponseStates.Draft => ApiSecondOpinionResponseStates.Draft,
                QuerySecondOpinionResponseStates.Published => ApiSecondOpinionResponseStates.Published,
                _ => throw new NotImplementedException()
            };

            if(response.SecondOpinion is not null && includeParent)
            {
                SecondOpinion = new ApiSecondOpinion(response.SecondOpinion, includeChildren: false);
            }
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public ApiPerson AssignedTo { get; set; } = null!;


        public DateTimeOffset? AnsweredAt { get; set; }

        public string? Comment { get; set; }
        public ApiSecondOpinionResponseStates State { get; set; }
        public ApiSecondOpinion? SecondOpinion { get; }
    }
}
