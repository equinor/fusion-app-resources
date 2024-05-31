using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(BinderType = typeof(OrgUnitResolver))]
    public class OrgUnitIdentifier
    {
        public OrgUnitIdentifier(string originalIdentifier, string sapId, string fullDepartment, string name)
        {
            OriginalIdentifier = originalIdentifier;
            SapId = sapId;
            FullDepartment = fullDepartment;
            Name = name;

            Exists = true;
        }

        private OrgUnitIdentifier(string originalIdentifier)
        {
            Exists = false;

            OriginalIdentifier = originalIdentifier;
            SapId = originalIdentifier;
            FullDepartment = originalIdentifier;
            Name = string.Empty;
        }

        [JsonIgnore]
        public string OriginalIdentifier { get; set; }
        [JsonIgnore]
        public string SapId { get; set; }
        [JsonIgnore]
        public string FullDepartment { get; set; }
        [JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        public bool Exists { get; set; }

        public static OrgUnitIdentifier NotFound(string identifier) => new OrgUnitIdentifier(identifier);
    }
}
