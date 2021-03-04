using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryRequestComment
    {
        public QueryRequestComment(DbRequestComment comment)
        {
            Enum.TryParse<QueryRequestOrigin>($"{comment.Origin}", out var queryOrigin);

            Id = comment.Id;

            Created = comment.Created;
            CreatedBy = new QueryPerson(comment.CreatedBy);
            Updated = comment.Updated;
            UpdatedBy = comment.UpdatedBy != null ? new QueryPerson(comment.UpdatedBy) : null;
            Content = comment.Comment;
            Origin = $"{queryOrigin}";
        }

        public Guid Id { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }

        public string Content { get; set; }
        public string Origin { get; set; }
    }
    public enum QueryRequestOrigin { Unknown, Company, Contractor, Local, Application }
}
