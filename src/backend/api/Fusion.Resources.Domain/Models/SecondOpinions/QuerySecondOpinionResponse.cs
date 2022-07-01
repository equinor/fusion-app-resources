using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public enum QuerySecondOpinionResponseStates { Open, Draft, Published, Closed }
    public class QuerySecondOpinionResponse
    {
        public QuerySecondOpinionResponse(DbSecondOpinionResponse response)
        {
            Id = response.Id;
            PromptId = response.PromptId;
            AssignedToId = response.AssignedToId;
            AssignedTo = new QueryPerson(response.AssignedTo);
            CreatedDate = response.CreatedDate;
            AnsweredAt = response.AnsweredAt;
            Comment = response.Comment;
            State = response.State switch
            {
                DbSecondOpinionResponseStates.Open => QuerySecondOpinionResponseStates.Open,
                DbSecondOpinionResponseStates.Draft => QuerySecondOpinionResponseStates.Draft,
                DbSecondOpinionResponseStates.Published => QuerySecondOpinionResponseStates.Published,
                DbSecondOpinionResponseStates.Closed => QuerySecondOpinionResponseStates.Closed,
                _ => throw new NotImplementedException()
            };
            SecondOpinion = new QuerySecondOpinion(response.SecondOpinion);
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public QueryPerson AssignedTo { get; set; } = null!;
        public DateTimeOffset CreatedDate { get; }

        public DateTimeOffset? AnsweredAt { get; set; }

        public string? Comment { get; set; }
        public QuerySecondOpinionResponseStates State { get; set; }
        public QuerySecondOpinion SecondOpinion { get; }
    }
}