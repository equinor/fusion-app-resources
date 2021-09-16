using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fusion.Resources.Domain
{
    public class QueryRequestAction
    {
        private readonly string? propertiesJson;


        public QueryRequestAction(DbRequestAction dbAction)
        {
            Id = dbAction.Id;
            Title = dbAction.Title;
            Body = dbAction.Body;
            Type = dbAction.Type;
            SubType = dbAction.SubType;
            Source = dbAction.Source.MapToDomain();
            Responsible = dbAction.Responsible.MapToDomain();

            IsResolved = dbAction.IsResolved;
            ResolvedAt = dbAction.ResolvedAt;
            ResolvedBy = (dbAction.ResolvedBy is not null) ? new QueryPerson(dbAction.ResolvedBy) : null;
            SentBy = new QueryPerson(dbAction.SentBy);
            IsRequired = dbAction.IsRequired;
            propertiesJson = dbAction.PropertiesJson;
            DueDate = dbAction.DueDate;
            AssignedTo = (dbAction.AssignedTo is not null) ? new QueryPerson(dbAction.AssignedTo) : null;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string? Body { get; }
        public string Type { get; }
        public string? SubType { get; }
        public QueryTaskSource Source { get; }
        public QueryTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public bool IsRequired { get; set; }
        public DateTimeOffset? ResolvedAt { get; }
        public QueryPerson? ResolvedBy { get; }
        public QueryPerson SentBy { get; }
        public QueryPerson? AssignedTo { get; }

        public DateTime? DueDate { get; }

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
