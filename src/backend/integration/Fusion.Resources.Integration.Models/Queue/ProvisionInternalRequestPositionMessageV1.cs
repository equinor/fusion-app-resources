using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Integration.Models.Queue
{
    /// <summary>
    /// Model must be backwards compatible, if fields are changed or removed in a way that will affect deserialization, a new version should be created.
    /// </summary>
    public class ProvisionInternalRequestPositionMessageV1
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }

        [JsonPropertyName("projectOrgId")]
        public Guid ProjectOrgId { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RequestTypeV1 Type { get; set; } = RequestTypeV1.Employee;


        /// <summary>
        /// The enum must be backwards compatible for deserialization. 
        /// If an enum needs to be renamed, a new version must be created, and conversion between 1 -> x must be handled.
        /// </summary>
        public enum RequestTypeV1 { Employee }
    }


}
