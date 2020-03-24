using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractPersonnelRequest
    {     
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
            Workflow = new ApiWorkflow(query.Workflow);
            ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);

            OriginalPositionId = query.OriginalPositionId;
            SetOriginalProperties(query);

            Category = query.Category switch
            {
                DbRequestCategory.ChangeRequest => ApiRequestCategory.ChangeRequest,
                DbRequestCategory.NewRequest => ApiRequestCategory.NewRequest,
                _ => throw new NotSupportedException($"Invalid request category {query.Category}")
            };
        }

        public Guid Id { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiRequestState State { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiRequestCategory Category { get; set; }

        public Guid? OriginalPositionId { get; set; }
        public ApiRequestPosition? OriginalPosition { get; set; }
        public ApiPerson? OriginalPerson { get; set; }

        public string Description { get; set; }

        public ApiRequestPosition Position { get; set; }
        public ApiContractPersonnel Person { get; set; }

        public ApiContractReference Contract { get; set; }
        public ApiProjectReference Project { get; set; }

        public List<ApiRequestComment>? Comments { get; set; }

        public ApiWorkflow Workflow { get; set; }
        public ApiProvisioningStatus ProvisioningStatus { get; set; }


        private void SetOriginalProperties(QueryPersonnelRequest query)
        {
            if (query.OriginalPositionId.HasValue && query.ResolvedOriginalPosition != null)
            {
                OriginalPosition = ApiRequestPosition.FromEntityOrDefault(query.ResolvedOriginalPosition);

                var instance = query.ResolvedOriginalPosition.Instances.FirstOrDefault();
                if (instance != null && instance.AssignedPerson != null)
                {
                    OriginalPerson = new ApiPerson(instance.AssignedPerson);
                }
            }
        }

        public enum ApiRequestState { Created, SubmittedToCompany, RejectedByContractor, ApprovedByCompany, RejectedByCompany }
        public enum ApiRequestCategory { NewRequest, ChangeRequest }
    }
}
