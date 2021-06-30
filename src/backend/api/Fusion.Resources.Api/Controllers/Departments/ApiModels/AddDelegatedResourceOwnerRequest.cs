using System;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class AddDelegatedResourceOwnerRequest
    {
        public Guid ResponsibleAzureUniqueId { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public DateTimeOffset DateFrom { get; set; }
    }
}
