using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fusion.Resources.Domain
{
    public class QueryRequestTask
    {
        private readonly string? propertiesJson;
        public QueryRequestTask(DbRequestTask dbTask)
        {
            Id = dbTask.Id;
            Title = dbTask.Title;
            Body = dbTask.Body;
            Type = dbTask.Type;
            SubType = dbTask.SubType;
            Source = dbTask.Source.MapToDomain();
            Responsible = dbTask.Responsible.MapToDomain();

            IsResolved = dbTask.IsResolved;
            ResolvedAt = dbTask.ResolvedAt;
            ResolvedBy = (dbTask.ResolvedBy is not null) ? new QueryPerson(dbTask.ResolvedBy) : null;
            propertiesJson = dbTask.PropertiesJson;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Type { get; }
        public string? SubType { get; }
        public QueryTaskSource Source { get; }
        public QueryTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public DateTimeOffset? ResolvedAt { get; }
        public QueryPerson? ResolvedBy { get; }

        public Dictionary<string, object> Properties
        {
            get
            {
                if (string.IsNullOrEmpty(propertiesJson)) return new();

                else return JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson) ?? new();
            }
        }
    }
}
