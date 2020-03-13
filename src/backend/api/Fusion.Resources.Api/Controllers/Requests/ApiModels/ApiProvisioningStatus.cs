using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProvisioningStatus
    {
        public ApiProvisioningStatus(QueryProvisioningStatus status)
        {
            PositionId = status.PositionId;
            Provisioned = status.Provisioned;
            ErrorMessage = status.ErrorMessage;
            ErrorPayload = status.ErrorPayload;

            if (Enum.TryParse(status.State, true, out ApiProvisionState state))
                State = state;
            else
                State = ApiProvisionState.Unknown;
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiProvisionState State { get; set; }
        public Guid? PositionId { get; set; }
        public DateTimeOffset? Provisioned { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorPayload { get; set; }

        public enum ApiProvisionState { NotProvisioned, Provisioned, Error, Unknown }

    }
}
