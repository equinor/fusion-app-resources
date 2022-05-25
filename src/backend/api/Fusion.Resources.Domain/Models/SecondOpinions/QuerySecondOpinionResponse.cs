using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
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
            State = response.State.ToString();
        }


        public Guid Id { get; set; }
        public Guid PromptId { get; set; }

        public Guid AssignedToId { get; set; }
        public QueryPerson AssignedTo { get; set; } = null!;


        public DateTimeOffset? AnsweredAt { get; set; }

        public string? Comment { get; set; }
        public string State { get; set; }
    }
}