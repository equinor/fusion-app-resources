using Fusion.Integration.Profile;
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
            RequestId = dbAction.RequestId;
            Title = dbAction.Title;
            Body = dbAction.Body;
            Type = dbAction.Type;
            SubType = dbAction.SubType;
            Source = dbAction.Source.MapToDomain();
            Responsible = dbAction.Responsible.MapToDomain();

            IsResolved = dbAction.IsResolved;
            ResolvedAt = dbAction.ResolvedAt;
            IsRequired = dbAction.IsRequired.GetValueOrDefault();
            propertiesJson = dbAction.PropertiesJson;
            DueDate = dbAction.DueDate;
        }

        public Guid Id { get; }
        public Guid RequestId { get; set; }
        public string Title { get; }
        public string? Body { get; }
        public string Type { get; }
        public string? SubType { get; }
        public QueryTaskSource Source { get; }
        public QueryTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public bool IsRequired { get; set; }
        public DateTimeOffset? ResolvedAt { get; }
        public FusionPersonProfile? ResolvedBy { get; set; }
        public FusionPersonProfile? SentBy { get; set; }
        public FusionPersonProfile? AssignedTo { get; set; }

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
