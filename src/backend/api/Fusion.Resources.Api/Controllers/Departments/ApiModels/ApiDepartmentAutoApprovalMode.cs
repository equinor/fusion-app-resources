using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// The modes for the auto approval, indicating if children/sub departments should be affected by the department setting.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiDepartmentAutoApprovalMode
    {
        All,
        Direct
    }
}
