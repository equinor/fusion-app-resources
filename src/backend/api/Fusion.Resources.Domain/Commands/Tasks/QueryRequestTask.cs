using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryRequestTask
    {
        public QueryRequestTask(DbRequestTask newTask)
        {
            Id = newTask.Id;
            Title = newTask.Title;
            Body = newTask.Body;
            Category = newTask.Category;
            Type = newTask.Type;
            SubType = newTask.SubType;
            Source = newTask.Source.MapToDomain();
            Responsible = newTask.Responsible.MapToDomain();
            
            IsResolved = newTask.IsResolved;
            ResolvedAt = newTask.ResolvedAt;
            ResolvedBy = (newTask.ResolvedBy is not null) ? new QueryPerson(newTask.ResolvedBy) : null;
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
