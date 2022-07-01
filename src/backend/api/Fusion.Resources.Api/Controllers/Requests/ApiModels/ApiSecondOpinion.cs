using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSecondOpinion
    {
        [JsonConverter(typeof(JsonStringEnumConverter))] 
        public enum ApiSecondOpinionStates { Active, Completed }

        public ApiSecondOpinion(QuerySecondOpinion query, Guid viewerAzureUniqueId, bool includeChildren = true)
        {
            Id = query.Id;
            Number = query.Number;
            Title = query.Title;
            Description = query.Description;
            CreatedById = query.CreatedById;
            CreatedBy = new ApiPerson(query.CreatedBy);
            CreatedDate = query.CreatedDate;

            if (query.Responses is not null && includeChildren)
            {
                Responses = query.Responses.Select(x => new ApiSecondOpinionResponse(x, viewerAzureUniqueId, includeParent: !includeChildren)).ToList();
            }
            if(query.Request is not null)
            {
                Request = new ApiResourceAllocationRequest(query.Request);
                if(query.Request.State?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true)
                {
                    State = ApiSecondOpinionStates.Completed;
                }
                else
                {
                    State = ApiSecondOpinionStates.Active;
                }
            }
        }

        public Guid Id { get; }
        public int Number { get; }
        public string Title { get; }
        public string Description { get; }


        public Guid CreatedById { get; }
        public ApiPerson CreatedBy { get; }
        public DateTimeOffset CreatedDate { get; }
        public List<ApiSecondOpinionResponse> Responses { get; } = new();
        public ApiResourceAllocationRequest? Request { get; }
        public ApiSecondOpinionStates? State { get; } 
    }
}
