using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public enum QuerySecondOpinionResponseStates { Open, Draft, Published }
    public class QuerySecondOpinionResponse
    {
        public QuerySecondOpinionResponse(DbSecondOpinionResponse response)
        {
            Id = response.Id;
            PromptId = response.PromptId;
            AssignedToId = response.AssignedToId;
            AssignedTo = new QueryPerson(response.AssignedTo);

            AnsweredAt = response.AnsweredAt;
            Comment = response.Comment;
            State = response.State switch
            {
                DbSecondOpinionResponseStates.Open => QuerySecondOpinionResponseStates.Open,
                DbSecondOpinionResponseStates.Draft => QuerySecondOpinionResponseStates.Draft,
                DbSecondOpinionResponseStates.Published => QuerySecondOpinionResponseStates.Published,
                _ => throw new NotImplementedException();
            };
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public QueryPerson AssignedTo { get; set; } = null!;


        public DateTimeOffset? AnsweredAt { get; set; }

        public string? Comment { get; set; }
        public QuerySecondOpinionResponseStates State { get; set; }
    }
}