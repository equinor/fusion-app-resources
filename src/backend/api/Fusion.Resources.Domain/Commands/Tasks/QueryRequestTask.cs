using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryRequestTask
    {
        public QueryRequestTask(DbRequestTask dbTask)
        {
            Id = dbTask.Id;
            Title = dbTask.Title;
            Body = dbTask.Body;
            Category = dbTask.Category;
            Type = dbTask.Type;
            SubType = dbTask.SubType;
            Source = dbTask.Source.MapToDomain();
            Responsible = dbTask.Responsible.MapToDomain();
            
            IsResolved = dbTask.IsResolved;
            ResolvedAt = dbTask.ResolvedAt;
            ResolvedBy = (dbTask.ResolvedBy is not null) ? new QueryPerson(dbTask.ResolvedBy) : null;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Category { get; }
        public string Type { get; }
        public string? SubType { get; }
        public TaskSource Source { get; }
        public TaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public DateTimeOffset? ResolvedAt { get; }
        public QueryPerson? ResolvedBy { get; }
    }
}
