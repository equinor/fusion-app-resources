using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractPersonnelRequest
    {
        public ApiContractPersonnelRequest()
        {

        }
        public ApiContractPersonnelRequest(QueryPersonnelRequest query)
        {
            Id = query.Id;
            Created = query.Created;
            Updated = query.Updated;
            CreatedBy = new ApiPerson(query.CreatedBy);
            UpdatedBy = ApiPerson.FromEntityOrDefault(query.UpdatedBy);

            State = Enum.Parse<ApiRequestState>($"{query.State}", true);
            Description = query.Description;

            Position = new ApiRequestPosition(query.Position);
            Person = new ApiContractPersonnel(query.Person);
            Project = new ApiProjectReference(query.Project);
            Contract = new ApiContractReference(query.Contract);
        }

        public Guid Id { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiRequestState State { get; set; }

        public string Description { get; set; }

        public ApiRequestPosition Position { get; set; }
        public ApiContractPersonnel Person { get; set; }

        public ApiContractReference Contract { get; set; }
        public ApiProjectReference Project { get; set; }

        public List<ApiRequestComment>? Comments { get; set; }

        public enum ApiRequestState { Created, Submitted, Approved, Rejected, Provisioned }
    }

}
