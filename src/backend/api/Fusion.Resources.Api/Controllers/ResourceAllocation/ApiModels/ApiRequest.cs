using System;
using System.Text.Json.Serialization;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequest
    {

        public ApiRequest()
        {
            
        }

        public ApiRequest(object arg)
        {
            Id = Guid.NewGuid();
            Discipline = "Yepp";
            Type = ApiRequestType.Normal;
            Workflow = new ApiWorkflow(new QueryWorkflow(new DbWorkflow()));
            Project = new ApiProjectReference(new QueryProject(new DbProject()));
            OriginalPositionId = Guid.NewGuid();
            OriginalPosition = new ApiRequestPosition(new QueryPositionRequest(new DbContractorRequest.RequestPosition()));
            AdditionalNote = "Whatever makes you happy";
            Created = DateTimeOffset.UtcNow;
            Updated = DateTimeOffset.UtcNow;
            LastActivity = DateTimeOffset.UtcNow;
            ProvisioningStatus =
                new ApiProvisioningStatus(new QueryProvisioningStatus(new DbContractorRequest.ProvisionStatus()));
        }

        public Guid Id { get; set; }
        public string Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiRequestType Type { get; set; }
        public ApiWorkflow Workflow { get; set; }
        public ApiProjectReference Project { get; set; }
        public Guid? OriginalPositionId { get; set; }
        public ApiRequestPosition? OriginalPosition { get; set; }
        public string AdditionalNote { get; set; }

        public object ProposedChanges { get; set; }
        public ApiPerson ProposedPerson { get; set; }
        
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset LastActivity { get; set; }

        public ApiProvisioningStatus ProvisioningStatus { get; set; }
       
        public enum ApiRequestType { Normal, JointVenture, Direct }
    }
}
